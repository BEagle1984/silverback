// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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

        private readonly ISilverbackIntegrationLogger<Producer> _logger;

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
        ///     The <see cref="ISilverbackIntegrationLogger" />.
        /// </param>
        protected Producer(
            IBroker broker,
            IProducerEndpoint endpoint,
            IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider,
            ISilverbackIntegrationLogger<Producer> logger)
        {
            Broker = Check.NotNull(broker, nameof(broker));
            Endpoint = Check.NotNull(endpoint, nameof(endpoint));
            _behaviors = Check.NotNull(behaviorsProvider, nameof(behaviorsProvider)).GetBehaviorsList();
            _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
            _logger = Check.NotNull(logger, nameof(logger));

            Endpoint.Validate();
        }

        /// <inheritdoc cref="IProducer.Id" />
        public Guid Id { get; } = Guid.NewGuid();

        /// <inheritdoc cref="IProducer.Broker" />
        public IBroker Broker { get; }

        /// <inheritdoc cref="IProducer.Endpoint" />
        public IProducerEndpoint Endpoint { get; }

        /// <inheritdoc cref="IProducer.IsConnected" />
        public bool IsConnected { get; private set; }

        /// <inheritdoc cref="IProducer.ConnectAsync" />
        public async Task ConnectAsync()
        {
            if (IsConnected)
                return;

            await ConnectCoreAsync().ConfigureAwait(false);

            IsConnected = true;
            _logger.LogDebug(
                IntegrationEventIds.ProducerConnected,
                "Connected producer to endpoint {endpoint}. (producerId: {producerId})",
                Endpoint.Name,
                Id);
        }

        /// <inheritdoc cref="IProducer.DisconnectAsync" />
        public async Task DisconnectAsync()
        {
            if (!IsConnected)
                return;

            await DisconnectCoreAsync().ConfigureAwait(false);

            IsConnected = false;
            _logger.LogDebug(
                IntegrationEventIds.ConsumerDisconnected,
                "Disconnected producer from endpoint {endpoint}. (producerId: {producerId})",
                Endpoint.Name,
                Id);
        }

        /// <inheritdoc cref="IProducer.Produce(object?,IReadOnlyCollection{MessageHeader}?)" />
        public void Produce(object? message, IReadOnlyCollection<MessageHeader>? headers = null) =>
            Produce(new OutboundEnvelope(message, headers, Endpoint));

        /// <inheritdoc cref="IProducer.Produce(IOutboundEnvelope)" />
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
        public void RawProduce(byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers = null)
            => Produce(new ProcessedOutboundEnvelope(messageContent, headers, Endpoint));

        /// <inheritdoc cref="IProducer.RawProduce(Stream?,IReadOnlyCollection{MessageHeader}?)" />
        public void RawProduce(Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers = null)
            => Produce(new ProcessedOutboundEnvelope(messageStream, headers, Endpoint));

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
        public Task RawProduceAsync(byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers = null)
            => ProduceAsync(new ProcessedOutboundEnvelope(messageContent, headers, Endpoint));

        /// <inheritdoc cref="IProducer.RawProduceAsync(Stream?,IReadOnlyCollection{MessageHeader}?)" />
        public Task RawProduceAsync(Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers = null)
            => ProduceAsync(new ProcessedOutboundEnvelope(messageStream, headers, Endpoint));

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

        private Task ExecutePipelineIfNeededAsync(
            ProducerPipelineContext context,
            ProducerBehaviorHandler finalAction)
        {
            if (context.Envelope is ProcessedOutboundEnvelope)
                return finalAction(context);

            return ExecutePipelineAsync(context, finalAction);
        }

        private async Task ExecutePipelineAsync(
            ProducerPipelineContext context,
            ProducerBehaviorHandler finalAction,
            int stepIndex = 0)
        {
            if (_behaviors.Count > 0 && stepIndex < _behaviors.Count)
            {
                await _behaviors[stepIndex].HandleAsync(
                        context,
                        nextContext => ExecutePipelineAsync(nextContext, finalAction, stepIndex + 1))
                    .ConfigureAwait(false);
            }
            else
            {
                await finalAction(context).ConfigureAwait(false);

                _logger.LogInformationWithMessageInfo(
                    IntegrationEventIds.MessageProduced,
                    "Message produced.",
                    context.Envelope);
            }
        }
    }
}
