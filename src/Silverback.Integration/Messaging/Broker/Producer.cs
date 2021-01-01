// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="IProducer" />
    public abstract class Producer : IProducer
    {
        private readonly IReadOnlyList<IProducerBehavior> _behaviors;

        private readonly IServiceProvider _serviceProvider;

        private readonly IOutboundLogger<Producer> _logger;

        private Task? _connectTask;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Producer" /> class.
        /// </summary>
        /// <param name="broker">
        ///     The <see cref="IBroker" /> that instantiated this producer.
        /// </param>
        /// <param name="endpoint">
        ///     The endpoint to produce to.
        /// </param>
        /// <param name="behaviorsProvider">
        ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}" />.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the needed services.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="IOutboundLogger{TCategoryName}" />.
        /// </param>
        protected Producer(
            IBroker broker,
            IProducerEndpoint endpoint,
            IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider,
            IOutboundLogger<Producer> logger)
        {
            Broker = Check.NotNull(broker, nameof(broker));
            Endpoint = Check.NotNull(endpoint, nameof(endpoint));
            _behaviors = Check.NotNull(behaviorsProvider, nameof(behaviorsProvider)).GetBehaviorsList();
            _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
            _logger = Check.NotNull(logger, nameof(logger));

            Endpoint.Validate();
        }

        /// <inheritdoc cref="IProducer.Id" />
        public InstanceIdentifier Id { get; } = new();

        /// <inheritdoc cref="IProducer.Broker" />
        public IBroker Broker { get; }

        /// <inheritdoc cref="IProducer.Endpoint" />
        public IProducerEndpoint Endpoint { get; }

        /// <inheritdoc cref="IProducer.IsConnected" />
        public bool IsConnected { get; private set; }

        /// <inheritdoc cref="IProducer.IsConnecting" />
        public bool IsConnecting => _connectTask != null;

        /// <inheritdoc cref="IProducer.ConnectAsync" />
        public async Task ConnectAsync()
        {
            if (IsConnected)
                return;

            if (_connectTask != null)
            {
                await _connectTask.ConfigureAwait(false);
                return;
            }

            _connectTask = ConnectCoreAsync();

            try
            {
                await _connectTask.ConfigureAwait(false);

                IsConnected = true;
            }
            finally
            {
                _connectTask = null;
            }

            _logger.LogProducerConnected(this);
        }

        /// <inheritdoc cref="IProducer.DisconnectAsync" />
        public async Task DisconnectAsync()
        {
            if (!IsConnected)
                return;

            await DisconnectCoreAsync().ConfigureAwait(false);

            IsConnected = false;
            _logger.LogProducerDisconnected(this);
        }

        /// <inheritdoc cref="IProducer.Produce(object?,IReadOnlyCollection{MessageHeader}?)" />
        public void Produce(object? message, IReadOnlyCollection<MessageHeader>? headers = null) =>
            Produce(new OutboundEnvelope(message, headers, Endpoint));

        /// <inheritdoc cref="IProducer.Produce(IOutboundEnvelope)" />
        [SuppressMessage("", "VSTHRD103", Justification = "Method executes synchronously")]
        public void Produce(IOutboundEnvelope envelope) =>
            AsyncHelper.RunSynchronously(
                async () =>
                {
                    await ConnectAsync().ConfigureAwait(false);

                    await ExecutePipelineIfNeededAsync(
                        new ProducerPipelineContext(envelope, this, _serviceProvider),
                        finalContext =>
                        {
                            ((RawOutboundEnvelope)finalContext.Envelope).BrokerMessageIdentifier =
                                ProduceCore(finalContext.Envelope);

                            return Task.CompletedTask;
                        }).ConfigureAwait(false);
                });

        /// <inheritdoc cref="IProducer.RawProduce(byte[],IReadOnlyCollection{MessageHeader}?)" />
        public void RawProduce(
            byte[]? messageContent,
            IReadOnlyCollection<MessageHeader>? headers = null)
            => Produce(new ProcessedOutboundEnvelope(messageContent, headers, Endpoint, Endpoint.Name));

        /// <inheritdoc cref="IProducer.RawProduce(Stream?,IReadOnlyCollection{MessageHeader}?)" />
        public void RawProduce(
            Stream? messageStream,
            IReadOnlyCollection<MessageHeader>? headers = null)
            => Produce(new ProcessedOutboundEnvelope(messageStream, headers, Endpoint, Endpoint.Name));

        /// <inheritdoc cref="IProducer.RawProduce(string, byte[],IReadOnlyCollection{MessageHeader}?)" />
        public void RawProduce(
            string actualEndpointName,
            byte[]? messageContent,
            IReadOnlyCollection<MessageHeader>? headers = null)
            => Produce(new ProcessedOutboundEnvelope(messageContent, headers, Endpoint, actualEndpointName));

        /// <inheritdoc cref="IProducer.RawProduce(string, Stream?,IReadOnlyCollection{MessageHeader}?)" />
        public void RawProduce(
            string actualEndpointName,
            Stream? messageStream,
            IReadOnlyCollection<MessageHeader>? headers = null)
            => Produce(new ProcessedOutboundEnvelope(messageStream, headers, Endpoint, actualEndpointName));

        /// <inheritdoc cref="IProducer.ProduceAsync(object?,IReadOnlyCollection{MessageHeader}?)" />
        public Task ProduceAsync(object? message, IReadOnlyCollection<MessageHeader>? headers = null) =>
            ProduceAsync(new OutboundEnvelope(message, headers, Endpoint));

        /// <inheritdoc cref="IProducer.ProduceAsync(IOutboundEnvelope)" />
        public async Task ProduceAsync(IOutboundEnvelope envelope)
        {
            await ConnectAsync().ConfigureAwait(false);

            await ExecutePipelineIfNeededAsync(
                new ProducerPipelineContext(envelope, this, _serviceProvider),
                async finalContext =>
                {
                    ((RawOutboundEnvelope)finalContext.Envelope).BrokerMessageIdentifier =
                        await ProduceCoreAsync(finalContext.Envelope).ConfigureAwait(false);
                }).ConfigureAwait(false);
        }

        /// <inheritdoc cref="IProducer.RawProduceAsync(byte[],IReadOnlyCollection{MessageHeader}?)" />
        public Task RawProduceAsync(
            byte[]? messageContent,
            IReadOnlyCollection<MessageHeader>? headers = null)
            => ProduceAsync(new ProcessedOutboundEnvelope(messageContent, headers, Endpoint, Endpoint.Name));

        /// <inheritdoc cref="IProducer.RawProduceAsync(Stream?,IReadOnlyCollection{MessageHeader}?)" />
        public Task RawProduceAsync(
            Stream? messageStream,
            IReadOnlyCollection<MessageHeader>? headers = null)
            => ProduceAsync(new ProcessedOutboundEnvelope(messageStream, headers, Endpoint, Endpoint.Name));

        /// <inheritdoc cref="IProducer.RawProduceAsync(string, byte[],IReadOnlyCollection{MessageHeader}?)" />
        public Task RawProduceAsync(
            string actualEndpointName,
            byte[]? messageContent,
            IReadOnlyCollection<MessageHeader>? headers = null)
            => ProduceAsync(
                new ProcessedOutboundEnvelope(messageContent, headers, Endpoint, actualEndpointName));

        /// <inheritdoc cref="IProducer.RawProduceAsync(string, Stream?,IReadOnlyCollection{MessageHeader}?)" />
        public Task RawProduceAsync(
            string actualEndpointName,
            Stream? messageStream,
            IReadOnlyCollection<MessageHeader>? headers = null)
            => ProduceAsync(
                new ProcessedOutboundEnvelope(messageStream, headers, Endpoint, actualEndpointName));

        /// <summary>
        ///     Connects to the message broker.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        protected virtual Task ConnectCoreAsync() => Task.CompletedTask;

        /// <summary>
        ///     Disconnects from the message broker.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        protected virtual Task DisconnectCoreAsync() => Task.CompletedTask;

        /// <summary>
        ///     Publishes the specified message and returns its identifier.
        /// </summary>
        /// <param name="envelope">
        ///     The <see cref="RawBrokerEnvelope" /> containing body, headers, endpoint, etc.
        /// </param>
        /// <returns>
        ///     The message identifier assigned by the broker (the Kafka offset or similar).
        /// </returns>
        protected abstract IBrokerMessageIdentifier? ProduceCore(IOutboundEnvelope envelope);

        /// <summary>
        ///     Publishes the specified message and returns its identifier.
        /// </summary>
        /// <param name="envelope">
        ///     The <see cref="RawBrokerEnvelope" /> containing body, headers, endpoint, etc.
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
        ///     message identifier assigned by the broker (the Kafka offset or similar).
        /// </returns>
        protected abstract Task<IBrokerMessageIdentifier?> ProduceCoreAsync(IOutboundEnvelope envelope);

        private async Task ExecutePipelineIfNeededAsync(
            ProducerPipelineContext context,
            ProducerBehaviorHandler finalAction)
        {
            if (context.Envelope is ProcessedOutboundEnvelope)
                await finalAction(context).ConfigureAwait(false);
            else
                await ExecutePipelineAsync(context, finalAction).ConfigureAwait(false);

            _logger.LogProduced(context.Envelope);
        }

        private Task ExecutePipelineAsync(
            ProducerPipelineContext context,
            ProducerBehaviorHandler finalAction,
            int stepIndex = 0)
        {
            if (_behaviors.Count <= 0 || stepIndex >= _behaviors.Count)
                return finalAction(context);

            return _behaviors[stepIndex].HandleAsync(
                context,
                nextContext => ExecutePipelineAsync(nextContext, finalAction, stepIndex + 1));
        }
    }
}
