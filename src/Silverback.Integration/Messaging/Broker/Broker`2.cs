// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;
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

        private const int MaxConnectParallelism = 2;

        private const int MaxDisconnectParallelism = 4;

        private readonly EndpointsConfiguratorsInvoker _endpointsConfiguratorsInvoker;

        private readonly ISilverbackIntegrationLogger _logger;

        private readonly IServiceProvider _serviceProvider;

        private ConcurrentBag<IConsumer>? _consumers = new();

        private ConcurrentDictionary<IEndpoint, IProducer>? _producers;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Broker{TProducerEndpoint, TConsumerEndpoint}" /> class.
        /// </summary>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
        /// </param>
        protected Broker(IServiceProvider serviceProvider)
        {
            _producers = new ConcurrentDictionary<IEndpoint, IProducer>();

            _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
            _endpointsConfiguratorsInvoker = _serviceProvider.GetRequiredService<EndpointsConfiguratorsInvoker>();
            _logger = _serviceProvider
                .GetRequiredService<ISilverbackIntegrationLogger<Broker<IProducerEndpoint, TConsumerEndpoint>>>();

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
                        IntegrationEventIds.CreatingNewProducer,
                        "Creating new producer for endpoint {endpointName}. (Total producers: {ProducerCount})",
                        endpoint.Name,
                        _producers.Count + 1);

                    return InstantiateProducer(
                        (TProducerEndpoint)endpoint,
                        _serviceProvider.GetRequiredService<IBrokerBehaviorsProvider<IProducerBehavior>>(),
                        _serviceProvider);
                });
        }

        /// <inheritdoc cref="IBroker.AddConsumer" />
        public virtual IConsumer AddConsumer(IConsumerEndpoint endpoint)
        {
            Check.NotNull(endpoint, nameof(endpoint));

            if (_consumers == null)
                throw new ObjectDisposedException(GetType().FullName);

            if (IsConnected)
                throw new InvalidOperationException(CannotCreateConsumerIfConnectedExceptionMessage);

            _logger.LogInformation(
                IntegrationEventIds.CreatingNewConsumer,
                "Creating new consumer for endpoint {endpointName}.",
                endpoint.Name);

            var consumer = InstantiateConsumer(
                (TConsumerEndpoint)endpoint,
                _serviceProvider.GetRequiredService<IBrokerBehaviorsProvider<IConsumerBehavior>>(),
                _serviceProvider);

            _consumers.Add(consumer);

            return consumer;
        }

        /// <inheritdoc cref="IBroker.ConnectAsync" />
        public async Task ConnectAsync()
        {
            if (IsConnected)
                return;

            if (_consumers == null)
                throw new ObjectDisposedException(GetType().FullName);

            _endpointsConfiguratorsInvoker.Invoke();

            _logger.LogDebug(
                IntegrationEventIds.BrokerConnecting,
                "Connecting to message broker ({broker})...",
                GetType().Name);

            await ConnectAsync(_consumers).ConfigureAwait(false);
            IsConnected = true;

            _logger.LogInformation(
                IntegrationEventIds.BrokerConnected,
                "Connected to message broker ({broker})!",
                GetType().Name);
        }

        /// <inheritdoc cref="IBroker.DisconnectAsync" />
        public async Task DisconnectAsync()
        {
            if (!IsConnected)
                return;

            if (_consumers == null)
                throw new ObjectDisposedException(GetType().FullName);

            _logger.LogDebug(
                IntegrationEventIds.BrokerDisconnecting,
                "Disconnecting from message broker ({broker})...",
                GetType().Name);

            await DisconnectAsync(_consumers).ConfigureAwait(false);
            IsConnected = false;

            _logger.LogInformation(
                IntegrationEventIds.BrokerDisconnected,
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
        /// <param name="behaviorsProvider">
        ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}" />.
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
            IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider);

        /// <summary>
        ///     Returns a new instance of <see cref="IConsumer" /> to subscribe to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">
        ///     The endpoint.
        /// </param>
        /// <param name="behaviorsProvider">
        ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}" />.
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
            IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider);

        /// <summary>
        ///     Connects all the consumers and starts consuming.
        /// </summary>
        /// <param name="consumers">
        ///     The consumers to be started.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        protected virtual Task ConnectAsync(IEnumerable<IConsumer> consumers) =>
            consumers.ParallelForEachAsync(consumer => consumer.ConnectAsync(), MaxConnectParallelism);

        /// <summary>
        ///     Disconnects all the consumers and stops consuming.
        /// </summary>
        /// <param name="consumers">
        ///     The consumers to be stopped.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        protected virtual Task DisconnectAsync(IEnumerable<IConsumer> consumers) =>
            consumers.ParallelForEachAsync(consumer => consumer.DisconnectAsync(), MaxDisconnectParallelism);

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
        ///     resources.
        /// </summary>
        /// <param name="disposing">
        ///     A value indicating whether the method has been called by the <c>Dispose</c> method and not from the
        ///     finalizer.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            AsyncHelper.RunSynchronously(DisconnectAsync);

            _consumers?.OfType<IDisposable>().ForEach(o => o.Dispose());
            _consumers = null;

            // ReSharper disable once SuspiciousTypeConversion.Global
            _producers?.Values.OfType<IDisposable>().ForEach(o => o.Dispose());
            _producers = null;
        }
    }
}
