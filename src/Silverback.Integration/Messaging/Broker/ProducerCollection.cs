// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

internal sealed class ProducerCollection : IProducerCollection, IAsyncDisposable
{
    private readonly ConcurrentBag<ProducerItem> _producers = [];

    private readonly ConcurrentDictionary<Type, IReadOnlyCollection<IProducer>> _producersByMessageType = new();

    private readonly ConcurrentDictionary<string, IProducer> _producersByEndpointName = new();

    private readonly ConcurrentDictionary<string, IProducer> _producersByEndpointFriendlyName = new();

    public int Count => _producers.Count;

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

    public IReadOnlyCollection<IProducer> GetProducersForMessage(Type messageType) =>
        _producersByMessageType.GetOrAdd(
            messageType,
            static (keyMessageType, producers) => GetProducersForMessage(producers, keyMessageType),
            _producers);

    public IEnumerator<IProducer> GetEnumerator() => _producers.Select(item => item.Producer).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public ValueTask DisposeAsync() => _producers.DisposeAllAsync();

    private static IProducer[] GetProducersForMessage(ConcurrentBag<ProducerItem> producers, Type messageType) =>
        producers
            .Where(
                item =>
                    item.IsRouting &&
                    item.Producer.EndpointConfiguration.MessageType.IsAssignableFrom(GetActualMessageType(messageType)))
            .Select(item => item.Producer)
            .ToArray();

    private static Type GetActualMessageType(Type messageType) =>
        typeof(ITombstone<object>).IsAssignableFrom(messageType)
            ? messageType.GetInterfaces().First(
                    interfaceType => interfaceType.IsGenericType &&
                                     interfaceType.GetGenericTypeDefinition() == typeof(ITombstone<>))
                .GenericTypeArguments[0]
            : messageType;

    private sealed record ProducerItem(IProducer Producer, bool IsRouting);
}
