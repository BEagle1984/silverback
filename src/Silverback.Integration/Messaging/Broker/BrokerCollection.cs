// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="IBrokerCollection" />
    public class BrokerCollection : IBrokerCollection
    {
        private readonly IReadOnlyCollection<IBroker> _brokers;
        private readonly ConcurrentDictionary<Type, IBroker> _producerEndpointTypeMap;
        private readonly ConcurrentDictionary<Type, IBroker> _consumerEndpointTypeMap;

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

        public IProducer GetProducer(
            IProducerEndpoint endpoint,
            IReadOnlyCollection<IProducerBehavior> behaviors = null)
        {
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));

            var endpointType = endpoint.GetType();

            return FindBroker(
                    broker => broker.ProducerEndpointType,
                    endpointType,
                    _producerEndpointTypeMap)
                .GetProducer(endpoint, behaviors);
        }

        public IConsumer GetConsumer(
            IConsumerEndpoint endpoint,
            IReadOnlyCollection<IConsumerBehavior> behaviors = null)
        {
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));

            var endpointType = endpoint.GetType();

            return FindBroker(
                    broker => broker.ConsumerEndpointType,
                    endpointType,
                    _consumerEndpointTypeMap)
                .GetConsumer(endpoint, behaviors);
        }

        public void Connect() => _brokers.ForEach(broker => broker.Connect());

        public void Disconnect() => _brokers.ForEach(broker => broker.Disconnect());

        private IBroker FindBroker(
            Func<IBroker, Type> endpointTypePropertySelector,
            Type endpointType,
            ConcurrentDictionary<Type, IBroker> endpointTypeMap) =>
            endpointTypeMap.GetOrAdd(endpointType,
                _ => _brokers.FirstOrDefault(
                         broker => endpointTypePropertySelector.Invoke(broker).IsAssignableFrom(endpointType)) ??
                     throw new InvalidOperationException(
                         $"No message broker could be found to handle the endpoint of type \"{endpointType.Name}\". " +
                         $"Please register the necessary IBroker implementation with the DI container."));

        #region IReadOnlyCollection implementation

        public IEnumerator<IBroker> GetEnumerator() => _brokers.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _brokers.GetEnumerator();

        public int Count => _brokers.Count;

        #endregion
    }
}