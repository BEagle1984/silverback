// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly ILogger<Producer> _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Producer" /> class.
        /// </summary>
        /// <param name="broker">
        ///     The <see cref="IBroker" /> that instantiated this producer.
        /// </param>
        /// <param name="endpoint"> The endpoint to produce to. </param>
        /// <param name="behaviors"> The behaviors to be added to the pipeline. </param>
        /// <param name="logger"> The <see cref="ILogger" />. </param>
        protected Producer(
            IBroker broker,
            IProducerEndpoint endpoint,
            IReadOnlyCollection<IProducerBehavior>? behaviors,
            ILogger<Producer> logger)
        {
            Behaviors = behaviors ?? Array.Empty<IProducerBehavior>();
            _logger = Check.NotNull(logger, nameof(logger));

            Broker = Check.NotNull(broker, nameof(broker));
            Endpoint = Check.NotNull(endpoint, nameof(endpoint));

            Endpoint.Validate();
        }

        /// <summary>
        ///     Gets the <see cref="IBroker" /> instance that owns this .
        /// </summary>
        public IBroker Broker { get; }

        /// <summary>
        ///     Gets the <see cref="IProducerEndpoint" /> this instance is connected to.
        /// </summary>
        public IProducerEndpoint Endpoint { get; }

        /// <inheritdoc />
        public IReadOnlyCollection<IProducerBehavior> Behaviors { get; }

        /// <inheritdoc />
        public void Produce(object? message, IReadOnlyCollection<MessageHeader>? headers = null) =>
            Produce(new OutboundEnvelope(message, headers, Endpoint));

        /// <inheritdoc />
        public Task ProduceAsync(object? message, IReadOnlyCollection<MessageHeader>? headers = null) =>
            ProduceAsync(new OutboundEnvelope(message, headers, Endpoint));

        /// <inheritdoc />
        public void Produce(IOutboundEnvelope envelope) =>
            AsyncHelper.RunSynchronously(
                () =>
                    ExecutePipeline(
                        Behaviors,
                        new ProducerPipelineContext(envelope, this),
                        finalContext =>
                        {
                            ((RawOutboundEnvelope)finalContext.Envelope).Offset =
                                ProduceCore(finalContext.Envelope);

                            return Task.CompletedTask;
                        }));

        /// <inheritdoc />
        public async Task ProduceAsync(IOutboundEnvelope envelope) =>
            await ExecutePipeline(
                Behaviors,
                new ProducerPipelineContext(envelope, this),
                async finalContext =>
                {
                    ((RawOutboundEnvelope)finalContext.Envelope).Offset =
                        await ProduceAsyncCore(finalContext.Envelope);
                });

        /// <summary>
        ///     Publishes the specified message and returns its offset.
        /// </summary>
        /// <param name="envelope">
        ///     The <see cref="RawBrokerEnvelope" /> containing body, headers, endpoint, etc.
        /// </param>
        /// <returns> The message offset. </returns>
        protected abstract IOffset ProduceCore(IRawOutboundEnvelope envelope);

        /// <summary>
        ///     Publishes the specified message and returns its offset.
        /// </summary>
        /// <param name="envelope">
        ///     The <see cref="RawBrokerEnvelope" /> containing body, headers, endpoint, etc.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation. The task result contains the
        ///     message offset.
        /// </returns>
        protected abstract Task<IOffset> ProduceAsyncCore(IRawOutboundEnvelope envelope);

        private async Task ExecutePipeline(
            IReadOnlyCollection<IProducerBehavior> behaviors,
            ProducerPipelineContext context,
            ProducerBehaviorHandler finalAction)
        {
            if (behaviors != null && behaviors.Any())
            {
                await behaviors.First()
                    .Handle(
                        context,
                        nextContext =>
                            ExecutePipeline(behaviors.Skip(1).ToList(), nextContext, finalAction));
            }
            else
            {
                await finalAction(context);
                _logger.LogInformation(EventIds.ProducerMessageProduced,  "Message produced.", context.Envelope);
            }
        }
    }
}
