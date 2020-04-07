// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="IProducer" />
    public abstract class Producer : IProducer
    {
        private readonly MessageLogger _messageLogger;
        private readonly ILogger<Producer> _logger;

        protected Producer(
            IBroker broker,
            IProducerEndpoint endpoint,
            IReadOnlyCollection<IProducerBehavior> behaviors,
            ILogger<Producer> logger,
            MessageLogger messageLogger)
        {
            Behaviors = behaviors ?? Array.Empty<IProducerBehavior>();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageLogger = messageLogger ?? throw new ArgumentNullException(nameof(messageLogger));

            Broker = broker ?? throw new ArgumentNullException(nameof(broker));
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

            Endpoint.Validate();
        }

        public IReadOnlyCollection<IProducerBehavior> Behaviors { get; }

        /// <summary>
        ///     Gets the <see cref="IBroker" /> instance that owns this instance.
        /// </summary>
        public IBroker Broker { get; }

        /// <summary>
        ///     Gets the <see cref="IProducerEndpoint" /> this instance is connected to.
        /// </summary>
        public IProducerEndpoint Endpoint { get; }

        public void Produce(object message, IReadOnlyCollection<MessageHeader> headers = null) =>
            Produce(new OutboundEnvelope(message, headers, Endpoint));

        public Task ProduceAsync(object message, IReadOnlyCollection<MessageHeader> headers = null) =>
            ProduceAsync(new OutboundEnvelope(message, headers, Endpoint));

        public void Produce(IOutboundEnvelope envelope) =>
            AsyncHelper.RunSynchronously(() =>
                ExecutePipeline(
                    Behaviors,
                    new ProducerPipelineContext(envelope, this),
                    finalContext =>
                    {
                        ((RawOutboundEnvelope) finalContext.Envelope).Offset =
                            ProduceImpl(finalContext.Envelope);

                        return Task.CompletedTask;
                    }));

        public async Task ProduceAsync(IOutboundEnvelope envelope) =>
            await ExecutePipeline(
                Behaviors,
                new ProducerPipelineContext(envelope, this),
                async finalContext =>
                {
                    ((RawOutboundEnvelope) finalContext.Envelope).Offset =
                        await ProduceAsyncImpl(finalContext.Envelope);
                });

        private async Task ExecutePipeline(
            IReadOnlyCollection<IProducerBehavior> behaviors,
            ProducerPipelineContext context,
            ProducerBehaviorHandler finalAction)
        {
            if (behaviors != null && behaviors.Any())
            {
                await behaviors.First()
                    .Handle(context, nextContext =>
                        ExecutePipeline(behaviors.Skip(1).ToList(), nextContext, finalAction));
            }
            else
            {
                await finalAction(context);
                _messageLogger.LogInformation(_logger, "Message produced.", context.Envelope);
            }
        }

        /// <summary>
        ///     Publishes the specified message and returns its offset.
        /// </summary>
        /// <param name="envelope">The <see cref="RawBrokerEnvelope" /> instance containing body, headers, endpoint, etc.</param>
        /// <returns>The message offset.</returns>
        protected abstract IOffset ProduceImpl(IRawOutboundEnvelope envelope);

        /// <summary>
        ///     Publishes the specified message and returns its offset.
        /// </summary>
        /// <param name="envelope">The <see cref="RawBrokerEnvelope" /> instance containing body, headers, endpoint, etc.</param>
        /// <returns>The message offset.</returns>
        protected abstract Task<IOffset> ProduceAsyncImpl(IRawOutboundEnvelope envelope);
    }

    /// <inheritdoc cref="Producer{TBroker,TEndpoint}" />
    public abstract class Producer<TBroker, TEndpoint> : Producer
        where TBroker : IBroker
        where TEndpoint : IProducerEndpoint
    {
        protected Producer(
            TBroker broker,
            TEndpoint endpoint,
            IReadOnlyCollection<IProducerBehavior> behaviors,
            ILogger<Producer> logger,
            MessageLogger messageLogger)
            : base(broker, endpoint, behaviors, logger, messageLogger)
        {
        }

        /// <summary>
        ///     Gets the <typeparamref name="TBroker" /> instance that owns this instance.
        /// </summary>
        protected new TBroker Broker => (TBroker) base.Broker;

        /// <summary>
        ///     Gets the <typeparamref name="TEndpoint" /> this instance is connected to.
        /// </summary>
        protected new TEndpoint Endpoint => (TEndpoint) base.Endpoint;
    }
}