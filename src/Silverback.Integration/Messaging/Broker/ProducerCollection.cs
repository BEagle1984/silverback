// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.EnrichedMessages;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

internal sealed class ProducerCollection : IProducerCollection, IAsyncDisposable
{
    private static readonly Type[] CompatibleGenericInterfaces =
    {
        typeof(ITombstone<>),
        typeof(IEnumerable<>),
        typeof(IAsyncEnumerable<>),
        typeof(IMessageWithHeaders<>)
    };

    private readonly List<ProducerItem> _producers = new();

    private readonly ConcurrentDictionary<Type, IReadOnlyCollection<IProducer>> _producersByMessageType = new();

    private readonly ConcurrentDictionary<string, IProducer> _producersByEndpointName = new();

    private readonly ConcurrentDictionary<string, IProducer> _producersByEndpointFriendlyName = new();

    public int Count => _producers.Count;

    public IProducer this[int index] => _producers[index].Producer;

    public void Add(IProducer producer, bool routing = true)
    {
        Check.NotNull(producer, nameof(producer));

        _producers.Add(new ProducerItem(producer, routing));
        _producersByEndpointName.TryAdd(producer.EndpointConfiguration.RawName, producer);

        if (!string.IsNullOrEmpty(producer.EndpointConfiguration.FriendlyName) &&
            !_producersByEndpointFriendlyName.TryAdd(producer.EndpointConfiguration.FriendlyName, producer))
        {
            throw new InvalidOperationException($"A producer endpoint with the name '{producer.EndpointConfiguration.FriendlyName}' has already been added.");
        }
    }

    public IProducer GetProducerForEndpoint(string endpointName)
    {
        if (_producersByEndpointFriendlyName.TryGetValue(endpointName, out IProducer? producer))
            return producer;

        if (_producersByEndpointName.TryGetValue(endpointName, out producer))
            return producer;

        throw new InvalidOperationException($"No producer has been configured for endpoint '{endpointName}'.");
    }

    public IReadOnlyCollection<IProducer> GetProducersForMessage(object message) =>
        GetProducersForMessage(message.GetType());

    public IReadOnlyCollection<IProducer> GetProducersForMessage(Type messageType) =>
        _producersByMessageType.GetOrAdd(
            messageType,
            static (keyMessageType, producers) => GetProducersForMessage(producers, keyMessageType),
            _producers);

    public IEnumerator<IProducer> GetEnumerator() => _producers.Select(item => item.Producer).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public ValueTask DisposeAsync() => _producers.DisposeAllAsync();

    private static IReadOnlyCollection<IProducer> GetProducersForMessage(List<ProducerItem> producers, Type messageType) =>
        producers
            .Where(
                item =>
                    item.IsRouting &&
                    item.Producer.EndpointConfiguration.MessageType.IsAssignableFrom(GetActualMessageType(messageType)))
            .Select(item => item.Producer)
            .ToList();

    private static Type GetActualMessageType(Type messageType) =>
        ExtractTypeFromSupportedInterface(messageType) ?? messageType;

    private static Type? ExtractTypeFromSupportedInterface(Type messageType)
    {
        Type? supportedInterfaceType = Array.Find(
            messageType.GetInterfaces(),
            interfaceType => interfaceType.IsGenericType && CompatibleGenericInterfaces.Contains(interfaceType.GetGenericTypeDefinition()));

        if (supportedInterfaceType == null)
            return null;

        Type innerMessageType = supportedInterfaceType.GenericTypeArguments[0];

        return ExtractTypeFromSupportedInterface(innerMessageType) ?? innerMessageType;
    }

    private sealed record ProducerItem(IProducer Producer, bool IsRouting);
}
