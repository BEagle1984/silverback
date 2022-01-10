// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

/// <inheritdoc cref="IBrokerCollection" />
public class BrokerCollection : IBrokerCollection
{
    private readonly IReadOnlyList<IBroker> _brokers;

    private readonly ConcurrentDictionary<Type, IBroker> _consumerEndpointTypeMap;

    private readonly ConcurrentDictionary<Type, IBroker> _producerEndpointTypeMap;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BrokerCollection" /> class.
    /// </summary>
    /// <param name="brokers">
    ///     The brokers to be added to the collection.
    /// </param>
    public BrokerCollection(IEnumerable<IBroker> brokers)
    {
        _brokers = brokers.ToList();

        _producerEndpointTypeMap = new ConcurrentDictionary<Type, IBroker>(
            _brokers.ToDictionary(
                broker => broker.ProducerConfigurationType,
                broker => broker));
        _consumerEndpointTypeMap = new ConcurrentDictionary<Type, IBroker>(
            _brokers.ToDictionary(
                broker => broker.ConsumerConfigurationType,
                broker => broker));
    }

    /// <inheritdoc cref="IReadOnlyCollection{T}.Count" />
    public int Count => _brokers.Count;

    /// <inheritdoc cref="IReadOnlyList{T}.this" />
    public IBroker this[int index] => _brokers[index];

    /// <inheritdoc cref="IBrokerCollection.GetProducerAsync" />
    public Task<IProducer> GetProducerAsync(ProducerConfiguration configuration)
    {
        Check.NotNull(configuration, nameof(configuration));

        Type endpointType = configuration.GetType();

        return FindBroker(
                broker => broker.ProducerConfigurationType,
                endpointType,
                _producerEndpointTypeMap)
            .GetProducerAsync(configuration);
    }

    /// <inheritdoc cref="IBrokerCollection.GetProducer" />
    public IProducer GetProducer(ProducerConfiguration configuration)
    {
        Check.NotNull(configuration, nameof(configuration));

        Type endpointType = configuration.GetType();

        return FindBroker(
                broker => broker.ProducerConfigurationType,
                endpointType,
                _producerEndpointTypeMap)
            .GetProducer(configuration);
    }

    /// <inheritdoc cref="IBrokerCollection.AddConsumer" />
    public IConsumer AddConsumer(ConsumerConfiguration configuration)
    {
        Check.NotNull(configuration, nameof(configuration));

        IBroker broker = FindBroker(
            broker => broker.ConsumerConfigurationType,
            configuration.GetType(),
            _consumerEndpointTypeMap);

        return broker.AddConsumer(configuration);
    }

    /// <inheritdoc cref="IBrokerCollection.ConnectAsync" />
    public Task ConnectAsync() => _brokers.ParallelForEachAsync(broker => broker.ConnectAsync(), 2);

    /// <inheritdoc cref="IBrokerCollection.DisconnectAsync" />
    public Task DisconnectAsync() => _brokers.ParallelForEachAsync(broker => broker.DisconnectAsync(), 2);

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator" />
    public IEnumerator<IBroker> GetEnumerator() => _brokers.GetEnumerator();

    /// <inheritdoc cref="IEnumerable.GetEnumerator" />
    IEnumerator IEnumerable.GetEnumerator() => _brokers.GetEnumerator();

    private IBroker FindBroker(
        Func<IBroker, Type> configurationTypePropertySelector,
        Type endpointType,
        ConcurrentDictionary<Type, IBroker> configurationTypeMap) =>
        configurationTypeMap.GetOrAdd(
            endpointType,
            static (keyEndpointType, args) =>
                args.Brokers.FirstOrDefault(
                    broker => args.ConfigurationTypePropertySelector.Invoke(broker)
                        .IsAssignableFrom(keyEndpointType)) ??
                throw new InvalidOperationException(
                    $"No message broker could be found to handle the endpoint of type {keyEndpointType.Name}. " +
                    "Please register the necessary IBroker implementation with the DI container."),
            (Brokers: _brokers, ConfigurationTypePropertySelector: configurationTypePropertySelector));
}
