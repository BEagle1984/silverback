// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     The base class for all <see cref="IBroker" /> implementations.
    /// </summary>
    /// <typeparam name="TProducerEndpoint">
    ///     The type of the <see cref="IProducerEndpoint" /> that is being handled by this
    ///     broker implementation.
    /// </typeparam>
    /// <typeparam name="TConsumerEndpoint">
    ///     The type of the <see cref="IConsumerEndpoint" /> that is being handled by this
    ///     broker implementation.
    /// </typeparam>
    public abstract class Broker<TProducerEndpoint, TConsumerEndpoint> : IBroker, IDisposable
        where TProducerEndpoint : IProducerEndpoint
        where TConsumerEndpoint : IConsumerEndpoint
    {
        private readonly List<IBrokerBehavior> _behaviors;
        private readonly ILogger _logger;

        private ConcurrentDictionary<IEndpoint, IProducer> _producers;
        private ConcurrentBag<IConsumer> _consumers = new ConcurrentBag<IConsumer>();

        private readonly IServiceProvider _serviceProvider;
        protected readonly ILoggerFactory LoggerFactory;

        protected Broker(
            IEnumerable<IBrokerBehavior> behaviors,
            ILoggerFactory loggerFactory,
            IServiceProvider serviceProvider)
        {
            _producers = new ConcurrentDictionary<IEndpoint, IProducer>();

            _behaviors = behaviors?.ToList() ?? new List<IBrokerBehavior>();

            LoggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = loggerFactory.CreateLogger(GetType());

            ProducerEndpointType = typeof(TProducerEndpoint);
            ConsumerEndpointType = typeof(TConsumerEndpoint);
        }

        #region Producer / Consumer

        /// <inheritdoc cref="IBroker" />
        public Type ProducerEndpointType { get; }

        /// <inheritdoc cref="IBroker" />
        public Type ConsumerEndpointType { get; }

        /// <inheritdoc cref="IBroker" />
        public virtual IProducer GetProducer(
            IProducerEndpoint endpoint,
            IReadOnlyCollection<IProducerBehavior> behaviors = null) =>
            _producers.GetOrAdd(endpoint, _ =>
            {
                _logger.LogInformation("Creating new producer for endpoint {endpointName}. " +
                                       $"(Total producers: {_producers.Count + 1})", endpoint.Name);
                return InstantiateProducer((TProducerEndpoint) endpoint, GetBehaviors(behaviors), _serviceProvider);
            });

        /// <summary>
        ///     Returns a new instance of <see cref="IProducer" /> to publish to the specified endpoint. The returned
        ///     instance will be cached and reused for the same endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="behaviors">The behaviors to be plugged-in.</param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> instance to be used to resolve the needed types or to be
        ///     forwarded to the consumer.
        /// </param>
        /// <returns></returns>
        protected abstract IProducer InstantiateProducer(
            TProducerEndpoint endpoint,
            IReadOnlyCollection<IProducerBehavior> behaviors,
            IServiceProvider serviceProvider);

        /// <inheritdoc cref="IBroker" />
        public virtual IConsumer GetConsumer(
            IConsumerEndpoint endpoint,
            IReadOnlyCollection<IConsumerBehavior> behaviors = null)
        {
            if (IsConnected)
                throw new InvalidOperationException(
                    "The broker is already connected. Disconnect it to get a new consumer.");

            _logger.LogInformation("Creating new consumer for endpoint {endpointName}.", endpoint.Name);

            var consumer = InstantiateConsumer((TConsumerEndpoint) endpoint, GetBehaviors(behaviors), _serviceProvider);

            _consumers.Add(consumer);

            return consumer;
        }

        /// <summary>
        ///     Returns a new instance of <see cref="IConsumer" /> to subscribe to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <param name="behaviors">The behaviors to be plugged-in.</param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> instance to be used to resolve the needed types or to be
        ///     forwarded to the consumer.
        /// </param>
        /// <returns></returns>
        protected abstract IConsumer InstantiateConsumer(
            TConsumerEndpoint endpoint,
            IReadOnlyCollection<IConsumerBehavior> behaviors,
            IServiceProvider serviceProvider);

        private IReadOnlyCollection<TBehavior> GetBehaviors<TBehavior>(IEnumerable<TBehavior> additionalBehaviors)
            where TBehavior : IBrokerBehavior =>
            GetBehaviors<TBehavior>().Union(additionalBehaviors ?? Array.Empty<TBehavior>()).SortBySortIndex()
                .ToList();

        private IEnumerable<TBehavior> GetBehaviors<TBehavior>()
            where TBehavior : IBrokerBehavior
        {
            foreach (var behavior in _behaviors)
            {
                switch (behavior)
                {
                    case TBehavior targetTypeBehavior:
                        yield return targetTypeBehavior;
                        break;
                    case IBrokerBehaviorFactory<TBehavior> behaviorFactory:
                        yield return behaviorFactory.Create();
                        break;
                }
            }
        }

        #endregion

        #region Connect / Disconnect

        /// <inheritdoc cref="IBroker" />
        public bool IsConnected { get; private set; }

        /// <inheritdoc cref="IBroker" />
        public void Connect()
        {
            if (IsConnected)
                return;

            _logger.LogDebug("Connecting to message broker ({broker})...", GetType().Name);

            Connect(_consumers);
            IsConnected = true;

            _logger.LogInformation("Connected to message broker ({broker})!", GetType().Name);
        }

        /// <summary>
        ///     Connects all the consumers and starts consuming.
        /// </summary>
        /// <param name="consumers"></param>
        protected virtual void Connect(IEnumerable<IConsumer> consumers) =>
            consumers.ForEach(c => c.Connect());

        /// <inheritdoc cref="IBroker" />
        public void Disconnect()
        {
            if (!IsConnected)
                return;

            _logger.LogDebug("Disconnecting from message broker ({broker})...", GetType().Name);

            Disconnect(_consumers);
            IsConnected = false;

            _logger.LogInformation("Disconnected from message broker ({broker})!", GetType().Name);
        }

        /// <summary>
        ///     Disconnects all the consumers and stops consuming.
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