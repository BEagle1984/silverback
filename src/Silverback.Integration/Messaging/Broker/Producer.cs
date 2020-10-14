// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
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

        /// <inheritdoc cref="IProducer.Broker" />
        public IBroker Broker { get; }

        /// <inheritdoc cref="IProducer.Endpoint" />
        public IProducerEndpoint Endpoint { get; }

        /// <inheritdoc cref="IProducer.Produce(object?,IReadOnlyCollection{MessageHeader}?)" />
        public void Produce(object? message, IReadOnlyCollection<MessageHeader>? headers = null) =>
            Produce(new OutboundEnvelope(message, headers, Endpoint));

        /// <inheritdoc cref="IProducer.Produce(IOutboundEnvelope)" />
        public void Produce(IOutboundEnvelope envelope) =>
            AsyncHelper.RunSynchronously(
                () =>
                    ExecutePipelineIfNeededAsync(
                        new ProducerPipelineContext(envelope, this, _serviceProvider),
                        finalContext =>
                        {
                            ((RawOutboundEnvelope)finalContext.Envelope).Offset =
                                ProduceCore(finalContext.Envelope);

                            return Task.CompletedTask;
                        }));

        /// <inheritdoc cref="IProducer.RawProduce(byte[]?,IReadOnlyCollection{MessageHeader}?)" />
        public void RawProduce(byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers = null)
            => Produce(new ProcessedOutboundEnvelope(messageContent, headers, Endpoint));

        /// <inheritdoc cref="IProducer.RawProduce(Stream?,IReadOnlyCollection{MessageHeader}?)" />
        public void RawProduce(Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers = null)
            => Produce(new ProcessedOutboundEnvelope(messageStream, headers, Endpoint));

        /// <inheritdoc cref="IProducer.ProduceAsync(object?,IReadOnlyCollection{MessageHeader}?)" />
        public Task ProduceAsync(object? message, IReadOnlyCollection<MessageHeader>? headers = null) =>
            ProduceAsync(new OutboundEnvelope(message, headers, Endpoint));

        /// <inheritdoc cref="IProducer.ProduceAsync(IOutboundEnvelope)" />
        public async Task ProduceAsync(IOutboundEnvelope envelope) =>
            await ExecutePipelineIfNeededAsync(
                new ProducerPipelineContext(envelope, this, _serviceProvider),
                async finalContext =>
                {
                    ((RawOutboundEnvelope)finalContext.Envelope).Offset =
                        await ProduceCoreAsync(finalContext.Envelope).ConfigureAwait(false);
                }).ConfigureAwait(false);

        /// <inheritdoc cref="IProducer.RawProduceAsync(byte[]?,IReadOnlyCollection{MessageHeader}?)" />
        public Task RawProduceAsync(byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers = null)
            => ProduceAsync(new ProcessedOutboundEnvelope(messageContent, headers, Endpoint));

        /// <inheritdoc cref="IProducer.RawProduceAsync(Stream?,IReadOnlyCollection{MessageHeader}?)" />
        public Task RawProduceAsync(Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers = null)
            => ProduceAsync(new ProcessedOutboundEnvelope(messageStream, headers, Endpoint));

        /// <summary>
        ///     Publishes the specified message and returns its offset.
        /// </summary>
        /// <param name="envelope">
        ///     The <see cref="RawBrokerEnvelope" /> containing body, headers, endpoint, etc.
        /// </param>
        /// <returns>
        ///     The message offset.
        /// </returns>
        protected abstract IOffset? ProduceCore(IOutboundEnvelope envelope);

        /// <summary>
        ///     Publishes the specified message and returns its offset.
        /// </summary>
        /// <param name="envelope">
        ///     The <see cref="RawBrokerEnvelope" /> containing body, headers, endpoint, etc.
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
        ///     message offset.
        /// </returns>
        protected abstract Task<IOffset?> ProduceCoreAsync(IOutboundEnvelope envelope);

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
                await _behaviors[stepIndex].Handle(
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
