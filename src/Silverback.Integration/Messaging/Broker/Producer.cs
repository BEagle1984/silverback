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
    public abstract class Producer : IProducer
    {
        private readonly IReadOnlyCollection<IProducerBehavior> _behaviors;
        private readonly MessageLogger _messageLogger;
        private readonly ILogger<Producer> _logger;

        protected Producer(
            IBroker broker,
            IProducerEndpoint endpoint,
            IEnumerable<IProducerBehavior> behaviors,
            ILogger<Producer> logger,
            MessageLogger messageLogger)
        {
            _behaviors = (IReadOnlyCollection<IProducerBehavior>) behaviors?.SortBySortIndex().ToList() ??
                         Array.Empty<IProducerBehavior>();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _messageLogger = messageLogger ?? throw new ArgumentNullException(nameof(messageLogger));

            Broker = broker ?? throw new ArgumentNullException(nameof(broker));
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

            Endpoint.Validate();
        }

        /// <summary>
        ///     Gets the <see cref="IBroker" /> instance that owns this instance.
        /// </summary>
        public IBroker Broker { get; }

        /// <summary>
        ///     Gets the <see cref="IProducerEndpoint" /> this instance is connected to.
        /// </summary>
        public IProducerEndpoint Endpoint { get; }

        /// <inheritdoc cref="IProducer" />
        public void Produce(object message, IReadOnlyCollection<MessageHeader> headers = null) =>
            Produce(new OutboundEnvelope(message, headers, Endpoint));

        /// <inheritdoc cref="IProducer" />
        public Task ProduceAsync(object message, IReadOnlyCollection<MessageHeader> headers = null) =>
            ProduceAsync(new OutboundEnvelope(message, headers, Endpoint));

        /// <inheritdoc cref="IProducer" />
        public void Produce(IOutboundEnvelope envelope) =>
            AsyncHelper.RunSynchronously(() =>
                ExecutePipeline(_behaviors, envelope, finalEnvelope =>
                {
                    ((RawOutboundEnvelope) finalEnvelope).Offset = ProduceImpl(finalEnvelope);
                    return Task.CompletedTask;
                }));

        /// <inheritdoc cref="IProducer" />
        public async Task ProduceAsync(IOutboundEnvelope envelope) =>
            await ExecutePipeline(_behaviors, envelope, async finalEnvelope =>
                ((RawOutboundEnvelope) finalEnvelope).Offset = await ProduceAsyncImpl(finalEnvelope));

        private async Task ExecutePipeline(
            IReadOnlyCollection<IProducerBehavior> behaviors,
            IOutboundEnvelope envelope,
            OutboundEnvelopeHandler finalAction)
        {
            if (behaviors != null && behaviors.Any())
            {
                await behaviors.First()
                    .Handle(envelope, m => ExecutePipeline(behaviors.Skip(1).ToList(), m, finalAction));
            }
            else
            {
                await finalAction(envelope);
                _messageLogger.LogInformation(_logger, "Message produced.", envelope);
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

    public abstract class Producer<TBroker, TEndpoint> : Producer
        where TBroker : IBroker
        where TEndpoint : IProducerEndpoint
    {
        protected Producer(
            TBroker broker,
            TEndpoint endpoint,
            IEnumerable<IProducerBehavior> behaviors,
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