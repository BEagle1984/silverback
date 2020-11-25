// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
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
                    broker => broker.ProducerEndpointType,
                    broker => broker));
            _consumerEndpointTypeMap = new ConcurrentDictionary<Type, IBroker>(
                _brokers.ToDictionary(
                    broker => broker.ConsumerEndpointType,
                    broker => broker));
        }

        /// <inheritdoc cref="IReadOnlyCollection{T}.Count" />
        public int Count => _brokers.Count;

        /// <inheritdoc cref="IReadOnlyList{T}.this" />
        public IBroker this[int index] => _brokers[index];

        /// <inheritdoc cref="IBrokerCollection.GetProducer" />
        public IProducer GetProducer(IProducerEndpoint endpoint)
        {
            Check.NotNull(endpoint, nameof(endpoint));

            var endpointType = endpoint.GetType();

            return FindBroker(
                    broker => broker.ProducerEndpointType,
                    endpointType,
                    _producerEndpointTypeMap)
                .GetProducer(endpoint);
        }

        /// <inheritdoc cref="IBrokerCollection.AddConsumer" />
        public IConsumer AddConsumer(IConsumerEndpoint endpoint)
        {
            Check.NotNull(endpoint, nameof(endpoint));

            var endpointType = endpoint.GetType();

            return FindBroker(
                    broker => broker.ConsumerEndpointType,
                    endpointType,
                    _consumerEndpointTypeMap)
                .AddConsumer(endpoint);
        }

        /// <inheritdoc cref="IBrokerCollection.Connect" />
        public void Connect() => _brokers.ParallelForEach(broker => broker.Connect(), 2);

        /// <inheritdoc cref="IBrokerCollection.Disconnect" />
        public void Disconnect() => _brokers.ParallelForEach(broker => broker.Disconnect(), 2);

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator" />
        public IEnumerator<IBroker> GetEnumerator() => _brokers.GetEnumerator();

        /// <inheritdoc cref="IEnumerable.GetEnumerator" />
        IEnumerator IEnumerable.GetEnumerator() => _brokers.GetEnumerator();

        private IBroker FindBroker(
            Func<IBroker, Type> endpointTypePropertySelector,
            Type endpointType,
            ConcurrentDictionary<Type, IBroker> endpointTypeMap) =>
            endpointTypeMap.GetOrAdd(
                endpointType,
                _ => _brokers.FirstOrDefault(
                         broker => endpointTypePropertySelector.Invoke(broker).IsAssignableFrom(endpointType)) ??
                     throw new InvalidOperationException(
                         $"No message broker could be found to handle the endpoint of type {endpointType.Name}. " +
                         "Please register the necessary IBroker implementation with the DI container."));
    }
}
