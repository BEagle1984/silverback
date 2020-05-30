// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     The base class for all <see cref="IBroker" /> implementations.
    /// </summary>
    /// <typeparam name="TProducerEndpoint">
    ///     The type of the <see cref="IProducerEndpoint" /> that is being handled by this broker
    ///     implementation.
    /// </typeparam>
    /// <typeparam name="TConsumerEndpoint">
    ///     The type of the <see cref="IConsumerEndpoint" /> that is being handled by this broker
    ///     implementation.
    /// </typeparam>
    [SuppressMessage("", "CA1724", Justification = "Preserve backward compatibility since no big deal")]
    public abstract class Broker<TProducerEndpoint, TConsumerEndpoint> : IBroker, IDisposable
        where TProducerEndpoint : IProducerEndpoint
        where TConsumerEndpoint : IConsumerEndpoint
    {
        private const string CannotCreateConsumerIfConnectedExceptionMessage =
            "The broker is already connected. Disconnect it to get a new consumer.";

        private readonly List<IBrokerBehavior> _behaviors;

        private readonly ILogger _logger;

        private readonly IServiceProvider _serviceProvider;

        private ConcurrentBag<IConsumer>? _consumers = new ConcurrentBag<IConsumer>();

        private ConcurrentDictionary<IEndpoint, IProducer>? _producers;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Broker{TProducerEndpoint, TConsumerEndpoint}" /> class.
        /// </summary>
        /// <param name="behaviors">
        ///     The <see cref="IEnumerable{T}" /> containing the <see cref="IBrokerBehavior" /> to be passed to the
        ///     producers and consumers.
        /// </param>
        /// <param name="loggerFactory">
        ///     The <see cref="ILoggerFactory" /> to be used to create the loggers.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
        /// </param>
        protected Broker(
            IEnumerable<IBrokerBehavior> behaviors,
            ILoggerFactory loggerFactory,
            IServiceProvider serviceProvider)
        {
            _producers = new ConcurrentDictionary<IEndpoint, IProducer>();

            _behaviors = behaviors?.ToList() ?? new List<IBrokerBehavior>();

            LoggerFactory = Check.NotNull(loggerFactory, nameof(loggerFactory));
            _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
            _logger = loggerFactory.CreateLogger(GetType());

            ProducerEndpointType = typeof(TProducerEndpoint);
            ConsumerEndpointType = typeof(TConsumerEndpoint);
        }

        /// <inheritdoc cref="IBroker.ProducerEndpointType" />
        public Type ProducerEndpointType { get; }

        /// <inheritdoc cref="IBroker.ConsumerEndpointType" />
        public Type ConsumerEndpointType { get; }

        /// <inheritdoc cref="IBroker.Producers" />
        public IReadOnlyList<IProducer> Producers
        {
            get
            {
                if (_producers == null)
                    throw new ObjectDisposedException(GetType().FullName);

                return _producers.Values.ToList().AsReadOnly();
            }
        }

        /// <inheritdoc cref="IBroker.Consumers" />
        public IReadOnlyList<IConsumer> Consumers
        {
            get
            {
                if (_consumers == null)
                    throw new ObjectDisposedException(GetType().FullName);

                return _consumers.ToList().AsReadOnly();
            }
        }

        /// <inheritdoc cref="IBroker.IsConnected" />
        public bool IsConnected { get; private set; }

        /// <summary>
        ///     Gets the <see cref="ILoggerFactory" />.
        /// </summary>
        protected ILoggerFactory LoggerFactory { get; }

        /// <inheritdoc cref="IBroker.GetProducer" />
        public virtual IProducer GetProducer(IProducerEndpoint endpoint)
        {
            Check.NotNull(endpoint, nameof(endpoint));

            if (_producers == null)
                throw new ObjectDisposedException(GetType().FullName);

            return _producers.GetOrAdd(
                endpoint,
                _ =>
                {
                    _logger.LogInformation(
                        EventIds.BrokerCreateProducer,
                        "Creating new producer for endpoint {endpointName}. (Total producers: {ProducerCount})",
                        endpoint.Name,
                        _producers.Count + 1);
                    return InstantiateProducer(
                        (TProducerEndpoint)endpoint,
                        GetBehaviors<IProducerBehavior>(),
                        _serviceProvider);
                });
        }

        /// <inheritdoc cref="IBroker.AddConsumer(IConsumerEndpoint,MessagesReceivedCallback)" />
        public virtual IConsumer AddConsumer(IConsumerEndpoint endpoint, MessagesReceivedCallback callback)
        {
            Check.NotNull(callback, nameof(callback));

            return AddConsumer(
                endpoint,
                args =>
                {
                    callback(args);
                    return Task.CompletedTask;
                });
        }

        /// <inheritdoc cref="IBroker.AddConsumer(IConsumerEndpoint,MessagesReceivedAsyncCallback)" />
        public virtual IConsumer AddConsumer(IConsumerEndpoint endpoint, MessagesReceivedAsyncCallback callback)
        {
            Check.NotNull(endpoint, nameof(endpoint));

            if (_consumers == null)
                throw new ObjectDisposedException(GetType().FullName);

            if (IsConnected)
                throw new InvalidOperationException(CannotCreateConsumerIfConnectedExceptionMessage);

            _logger.LogInformation(
                EventIds.BrokerCreatingConsumer,
                "Creating new consumer for endpoint {endpointName}.",
                endpoint.Name);

            var consumer = InstantiateConsumer(
                (TConsumerEndpoint)endpoint,
                callback,
                GetBehaviors<IConsumerBehavior>(),
                _serviceProvider);

            _consumers.Add(consumer);

            return consumer;
        }

        /// <inheritdoc cref="IBroker.Connect" />
        public void Connect()
        {
            if (IsConnected)
                return;

            if (_consumers == null)
                throw new ObjectDisposedException(GetType().FullName);

            _logger.LogDebug(EventIds.BrokerConnecting, "Connecting to message broker ({broker})...", GetType().Name);

            Connect(_consumers);
            IsConnected = true;

            _logger.LogInformation(EventIds.BrokerConnected, "Connected to message broker ({broker})!", GetType().Name);
        }

        /// <inheritdoc cref="IBroker.Disconnect" />
        public void Disconnect()
        {
            if (!IsConnected)
                return;

            if (_consumers == null)
                throw new ObjectDisposedException(GetType().FullName);

            _logger.LogDebug(
                EventIds.BrokerDisconnecting,
                "Disconnecting from message broker ({broker})...",
                GetType().Name);

            Disconnect(_consumers);
            IsConnected = false;

            _logger.LogInformation(
                EventIds.BrokerDisconnected,
                "Disconnected from message broker ({broker})!",
                GetType().Name);
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Returns a new instance of <see cref="IProducer" /> to publish to the specified endpoint. The
        ///     returned instance will be cached and reused for the same endpoint.
        /// </summary>
        /// <param name="endpoint">
        ///     The endpoint.
        /// </param>
        /// <param name="behaviors">
        ///     The behaviors to be plugged-in.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> instance to be used to resolve the needed types or to be
        ///     forwarded to the consumer.
        /// </param>
        /// <returns>
        ///     The instantiated <see cref="IProducer" />.
        /// </returns>
        protected abstract IProducer InstantiateProducer(
            TProducerEndpoint endpoint,
            IReadOnlyCollection<IProducerBehavior>? behaviors,
            IServiceProvider serviceProvider);

        /// <summary>
        ///     Returns a new instance of <see cref="IConsumer" /> to subscribe to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">
        ///     The endpoint.
        /// </param>
        /// <param name="callback">
        ///     The delegate to be invoked when a message is received.
        /// </param>
        /// <param name="behaviors">
        ///     The behaviors to be plugged-in.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> instance to be used to resolve the needed types or to be
        ///     forwarded to the consumer.
        /// </param>
        /// <returns>
        ///     The instantiated <see cref="IConsumer" />.
        /// </returns>
        protected abstract IConsumer InstantiateConsumer(
            TConsumerEndpoint endpoint,
            MessagesReceivedAsyncCallback callback,
            IReadOnlyCollection<IConsumerBehavior>? behaviors,
            IServiceProvider serviceProvider);

        /// <summary>
        ///     Connects all the consumers and starts consuming.
        /// </summary>
        /// <param name="consumers">
        ///     The consumers to be started.
        /// </param>
        protected virtual void Connect(IEnumerable<IConsumer> consumers) =>
            consumers.ForEach(c => c.Connect());

        /// <summary>
        ///     Disconnects all the consumers and stops consuming.
        /// </summary>
        /// <param name="consumers">
        ///     The consumers to be stopped.
        /// </param>
        protected virtual void Disconnect(IEnumerable<IConsumer> consumers) =>
            consumers.ForEach(c => c.Disconnect());

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
        ///     resources.
        /// </summary>
        /// <param name="disposing">
        ///     A value indicating whether the method has been called by the <c>Dispose</c> method and not from the finalizer.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            Disconnect();

            _consumers?.OfType<IDisposable>().ForEach(o => o.Dispose());
            _consumers = null;

            _producers?.Values.OfType<IDisposable>().ForEach(o => o.Dispose());
            _producers = null;
        }

        private IReadOnlyCollection<TBehavior> GetBehaviors<TBehavior>()
            where TBehavior : IBrokerBehavior =>
            GetBehaviorsEnumerable<TBehavior>()
                .SortBySortIndex()
                .ToList();

        private IEnumerable<TBehavior> GetBehaviorsEnumerable<TBehavior>()
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
    }
}
