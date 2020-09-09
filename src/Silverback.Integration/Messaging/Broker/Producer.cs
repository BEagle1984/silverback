// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
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
        private readonly IBrokerBehaviorsProvider<IProducerBehavior> _behaviorsProvider;

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
            _behaviorsProvider = Check.NotNull(behaviorsProvider, nameof(behaviorsProvider));
            _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
            _logger = Check.NotNull(logger, nameof(logger));

            Endpoint.Validate();
        }

        /// <inheritdoc cref="IProducer.Broker" />
        public IBroker Broker { get; }

        /// <inheritdoc cref="IProducer.Endpoint" />
        public IProducerEndpoint Endpoint { get; }

        /// <inheritdoc cref="IProducer.Produce(object?,IReadOnlyCollection{MessageHeader}?,bool)" />
        public void Produce(
            object? message,
            IReadOnlyCollection<MessageHeader>? headers = null,
            bool disableBehaviors = false) =>
            Produce(new OutboundEnvelope(message, headers, Endpoint), disableBehaviors);

        /// <inheritdoc cref="IProducer.Produce(IOutboundEnvelope,bool)" />
        public void Produce(IOutboundEnvelope envelope, bool disableBehaviors = false) =>
            AsyncHelper.RunSynchronously(
                () =>
                    ExecutePipeline(
                        _behaviorsProvider.CreateStack(),
                        new ProducerPipelineContext(envelope, this, _serviceProvider),
                        finalContext =>
                        {
                            ((RawOutboundEnvelope)finalContext.Envelope).Offset =
                                ProduceCore(finalContext.Envelope);

                            return Task.CompletedTask;
                        }));

        /// <inheritdoc cref="IProducer.ProduceAsync(object?,IReadOnlyCollection{MessageHeader}?,bool)" />
        public Task ProduceAsync(
            object? message,
            IReadOnlyCollection<MessageHeader>? headers = null,
            bool disableBehaviors = false) =>
            ProduceAsync(new OutboundEnvelope(message, headers, Endpoint), disableBehaviors);

        /// <inheritdoc cref="IProducer.ProduceAsync(IOutboundEnvelope,bool)" />
        public async Task ProduceAsync(IOutboundEnvelope envelope, bool disableBehaviors = false) =>
            await ExecutePipeline(
                _behaviorsProvider.CreateStack(),
                new ProducerPipelineContext(envelope, this, _serviceProvider),
                async finalContext =>
                {
                    ((RawOutboundEnvelope)finalContext.Envelope).Offset =
                        await ProduceAsyncCore(finalContext.Envelope).ConfigureAwait(false);
                }).ConfigureAwait(false);

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
        ///     A <see cref="Task" /> representing the asynchronous operation. The task result contains the message
        ///     offset.
        /// </returns>
        protected abstract Task<IOffset?> ProduceAsyncCore(IOutboundEnvelope envelope);

        private async Task ExecutePipeline(
            Stack<IProducerBehavior>? behaviors,
            ProducerPipelineContext context,
            ProducerBehaviorHandler finalAction)
        {
            if (behaviors != null && behaviors.TryPop(out var nextBehavior))
            {
                await nextBehavior
                    .Handle(
                        context,
                        nextContext =>
                            ExecutePipeline(behaviors, nextContext, finalAction))
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
