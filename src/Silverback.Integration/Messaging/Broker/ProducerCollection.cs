// Copyright (c) 2023 Sergio Aquilini
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
    private readonly List<ProducerItem> _producers = new();

    private readonly ConcurrentDictionary<Type, IReadOnlyCollection<IProducer>> _producersByMessageType = new();

    private readonly ConcurrentDictionary<string, IProducer> _producersByEndpointName = new();

    public int Count => _producers.Count;

    public IProducer this[int index] => _producers[index].Producer;

    public void Add(IProducer producer, bool routing = true)
    {
        Check.NotNull(producer, nameof(producer));

        _producers.Add(new ProducerItem(producer, routing));
        _producersByEndpointName.TryAdd(producer.EndpointConfiguration.RawName, producer);

        if (producer.EndpointConfiguration.FriendlyName != null)
            _producersByEndpointName.TryAdd(producer.EndpointConfiguration.FriendlyName, producer);
    }

    public IProducer GetProducerForEndpoint(string endpointName) =>
        _producersByEndpointName.TryGetValue(endpointName, out IProducer? producer)
            ? producer
            : throw new InvalidOperationException($"No producer has been configured for endpoint '{endpointName}'.");

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
                    IsCompatibleMessageType(item.Producer.EndpointConfiguration.MessageType, messageType) ||
                    IsCompatibleTombstone(item.Producer.EndpointConfiguration.MessageType, messageType))
            .Select(item => item.Producer)
            .ToList();

    private static bool IsCompatibleMessageType(Type producerType, Type messageType) =>
        producerType.IsAssignableFrom(messageType);

    private static bool IsCompatibleTombstone(Type producerType, Type messageType) =>
        typeof(Tombstone).IsAssignableFrom(messageType) &&
        messageType.GenericTypeArguments.Length == 1 &&
        producerType.IsAssignableFrom(messageType.GenericTypeArguments[0]);

    private sealed record ProducerItem(IProducer Producer, bool IsRouting);
}
