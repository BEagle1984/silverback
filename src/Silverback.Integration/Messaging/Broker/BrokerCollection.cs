// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="IBrokerCollection" />
    public class BrokerCollection : IBrokerCollection
    {
        private readonly IReadOnlyCollection<IBroker> _brokers;

        private readonly ConcurrentDictionary<Type, IBroker> _consumerEndpointTypeMap;

        private readonly ConcurrentDictionary<Type, IBroker> _producerEndpointTypeMap;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BrokerCollection" /> class.
        /// </summary>
        /// <param name="brokers"> The brokers to be added to the collection. </param>
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

        /// <summary>
        ///     Gets the number of brokers in the collection.
        /// </summary>
        public int Count => _brokers.Count;

        /// <inheritdoc />
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

        /// <inheritdoc />
        public IConsumer GetConsumer(IConsumerEndpoint endpoint, MessagesReceivedCallback callback)
        {
            Check.NotNull(callback, nameof(callback));

            return GetConsumer(
                endpoint,
                args =>
                {
                    callback(args);
                    return Task.CompletedTask;
                });
        }

        /// <inheritdoc />
        public IConsumer GetConsumer(IConsumerEndpoint endpoint, MessagesReceivedAsyncCallback callback)
        {
            Check.NotNull(endpoint, nameof(endpoint));

            var endpointType = endpoint.GetType();

            return FindBroker(
                    broker => broker.ConsumerEndpointType,
                    endpointType,
                    _consumerEndpointTypeMap)
                .GetConsumer(endpoint, callback);
        }

        /// <inheritdoc />
        public void Connect() => _brokers.ForEach(broker => broker.Connect());

        /// <inheritdoc />
        public void Disconnect() => _brokers.ForEach(broker => broker.Disconnect());

        /// <inheritdoc />
        public IEnumerator<IBroker> GetEnumerator() => _brokers.GetEnumerator();

        /// <inheritdoc />
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
                         $"No message broker could be found to handle the endpoint of type \"{endpointType.Name}\". " +
                         "Please register the necessary IBroker implementation with the DI container."));
    }
}
