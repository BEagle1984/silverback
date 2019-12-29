// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    public abstract class Broker : IBroker, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IEnumerable<IProducerBehavior> _producerBehaviors;
        private readonly IEnumerable<IConsumerBehavior> _consumerBehaviors;

        private ConcurrentDictionary<IEndpoint, IProducer> _producers = new ConcurrentDictionary<IEndpoint, IProducer>();
        private List<IConsumer> _consumers = new List<IConsumer>();

        protected readonly ILoggerFactory LoggerFactory;

        protected Broker(IEnumerable<IBrokerBehavior> behaviors, ILoggerFactory loggerFactory)
        {
            _producerBehaviors = behaviors?.OfType<IProducerBehavior>() ?? Enumerable.Empty<IProducerBehavior>();
            _consumerBehaviors = behaviors?.OfType<IConsumerBehavior>() ?? Enumerable.Empty<IConsumerBehavior>();

            LoggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger(GetType());
        }

        #region Producer / Consumer

        /// <inheritdoc cref="IBroker"/>
        public virtual IProducer GetProducer(IProducerEndpoint endpoint) =>
            _producers.GetOrAdd(endpoint, _ =>
            {
                _logger.LogInformation("Creating new producer for endpoint {endpointName}. " +
                                       $"(Total producers: {_producers.Count + 1})", endpoint.Name);
                return InstantiateProducer(endpoint, _producerBehaviors);
            });

        /// <summary>
        /// Returns a new instance of <see cref="IProducer"/> to publish to the specified endpoint. The returned
        /// instance will be cached and reused for the same endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="behaviors">The behaviors to be plugged-in.</param>
        /// <returns></returns>
        protected abstract IProducer InstantiateProducer(IProducerEndpoint endpoint, IEnumerable<IProducerBehavior> behaviors);

        /// <inheritdoc cref="IBroker"/>
        public virtual IConsumer GetConsumer(IConsumerEndpoint endpoint)
        {
            if (IsConnected)
                throw new InvalidOperationException("The broker is already connected. Disconnect it to get a new consumer.");

            _logger.LogInformation("Creating new consumer for endpoint {endpointName}.", endpoint.Name);

            var consumer = InstantiateConsumer(endpoint, _consumerBehaviors);

            lock (_consumers)
            {
                _consumers.Add(consumer);
            }

            return consumer;
        }

        /// <summary>
        /// Returns a new instance of <see cref="IConsumer"/> to subscribe to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="behaviors">The behaviors to be plugged-in.</param>
        /// <returns></returns>
        protected abstract IConsumer InstantiateConsumer(IConsumerEndpoint endpoint, IEnumerable<IConsumerBehavior> behaviors);

        #endregion

        #region Connect / Disconnect

        /// <inheritdoc cref="IBroker"/>
        public bool IsConnected { get; private set; }

        /// <inheritdoc cref="IBroker"/>
        public void Connect()
        {
            if (IsConnected)
                return;

            _logger.LogTrace("Connecting to message broker...");

            Connect(_consumers);
            IsConnected = true;

            _logger.LogTrace("Connected to message broker!");
        }

        /// <summary>
        /// Connects all the consumers and starts consuming.
        /// </summary>
        /// <param name="consumers"></param>
        protected virtual void Connect(IEnumerable<IConsumer> consumers) =>
            consumers.ForEach(c => c.Connect());

        /// <inheritdoc cref="IBroker"/>
        public void Disconnect()
        {
            if (!IsConnected)
                return;

            _logger.LogTrace("Disconnecting from message broker...");

            Disconnect(_consumers);
            IsConnected = false;

            _logger.LogTrace("Disconnected from message broker!");
        }

        /// <summary>
        /// Disconnects all the consumers and stops consuming.
        /// </summary>
        /// <param name="consumers"></param>
        protected virtual void Disconnect(IEnumerable<IConsumer> consumers) =>
            consumers.ForEach(c => c.Disconnect());

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            Disconnect();

            _consumers?.OfType<IDisposable>().ForEach(o => o.Dispose());
            _consumers = null;

            _producers?.Values.OfType<IDisposable>().ForEach(o => o.Dispose());
            _producers = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}