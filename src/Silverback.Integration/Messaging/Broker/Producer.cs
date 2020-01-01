// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    public abstract class Producer : IProducer
    {
        private readonly MessageKeyProvider _messageKeyProvider;
        private readonly IReadOnlyCollection<IProducerBehavior> _behaviors;
        private readonly MessageLogger _messageLogger;
        private readonly ILogger<Producer> _logger;

        protected Producer(
            IBroker broker,
            IProducerEndpoint endpoint,
            MessageKeyProvider messageKeyProvider,
            IEnumerable<IProducerBehavior> behaviors,
            ILogger<Producer> logger,
            MessageLogger messageLogger)
        {
            _messageKeyProvider = messageKeyProvider ?? throw new ArgumentNullException(nameof(messageKeyProvider));
            _behaviors = (IReadOnlyCollection<IProducerBehavior>) behaviors?.ToList() ??
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
            GetRawMessages(message, headers)
                .ForEach(rawMessage =>
                    ExecutePipeline(_behaviors, rawMessage, x =>
                    {
                        x.Offset = Produce(x);
                        return Task.CompletedTask;
                    }).Wait());

        /// <inheritdoc cref="IProducer" />
        public Task ProduceAsync(object message, IReadOnlyCollection<MessageHeader> headers = null) =>
            GetRawMessages(message, headers)
                .ForEachAsync(async rawMessage =>
                    await ExecutePipeline(_behaviors, rawMessage, async x =>
                        x.Offset = await ProduceAsync(x)));

        private IEnumerable<RawOutboundMessage> GetRawMessages(object content, IEnumerable<MessageHeader> headers)
        {
            var headersCollection = new MessageHeaderCollection(headers);
            _messageKeyProvider.EnsureKeyIsInitialized(content, headersCollection);
            var rawMessage = new RawOutboundMessage(content, headersCollection, Endpoint);

            return ChunkProducer.ChunkIfNeeded(rawMessage);
        }

        private async Task ExecutePipeline(
            IReadOnlyCollection<IProducerBehavior> behaviors,
            RawBrokerMessage message,
            RawBrokerMessageHandler finalAction)
        {
            if (behaviors != null && behaviors.Any())
            {
                await behaviors.First()
                    .Handle(message, m => ExecutePipeline(behaviors.Skip(1).ToList(), m, finalAction));
            }
            else
            {
                await finalAction(message);
                _messageLogger.LogInformation(_logger, "Message produced.", message);
            }
        }

        /// <summary>
        ///     Publishes the specified message and returns its offset.
        /// </summary>
        /// <param name="message">The <see cref="RawBrokerMessage" /> instance containing body, headers, endpoint, etc.</param>
        /// <returns>The message offset.</returns>
        protected abstract IOffset Produce(RawBrokerMessage message);

        /// <summary>
        ///     Publishes the specified message and returns its offset.
        /// </summary>
        /// <param name="message">The <see cref="RawBrokerMessage" /> instance containing body, headers, endpoint, etc.</param>
        /// <returns>The message offset.</returns>
        protected abstract Task<IOffset> ProduceAsync(RawBrokerMessage message);
    }

    public abstract class Producer<TBroker, TEndpoint> : Producer
        where TBroker : IBroker
        where TEndpoint : IProducerEndpoint
    {
        protected Producer(
            TBroker broker,
            TEndpoint endpoint,
            MessageKeyProvider messageKeyProvider,
            IEnumerable<IProducerBehavior> behaviors,
            ILogger<Producer> logger,
            MessageLogger messageLogger)
            : base(broker, endpoint, messageKeyProvider, behaviors, logger, messageLogger)
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