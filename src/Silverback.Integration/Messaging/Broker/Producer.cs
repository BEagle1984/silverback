// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    public abstract class Producer : EndpointConnectedObject, IProducer
    {
        private readonly MessageKeyProvider _messageKeyProvider;
        private readonly IEnumerable<IProducerBehavior> _behaviors;
        private readonly MessageLogger _messageLogger;
        private readonly ILogger<Producer> _logger;

        protected Producer(
            IBroker broker, 
            IEndpoint endpoint, 
            MessageKeyProvider messageKeyProvider,
            IEnumerable<IProducerBehavior> behaviors,
            ILogger<Producer> logger, 
            MessageLogger messageLogger)
            : base(broker, endpoint)
        {
            _messageKeyProvider = messageKeyProvider;
            _behaviors = behaviors;
            _logger = logger;
            _messageLogger = messageLogger;
        }

        public void Produce(object message, IEnumerable<MessageHeader> headers = null) =>
            GetRawMessages(message, headers)
                .ForEach(rawMessage =>
                    ExecutePipeline(_behaviors, rawMessage, x =>
                    {
                        x.Offset = Produce(x);
                        return Task.CompletedTask;
                    }).Wait());

        public Task ProduceAsync(object message, IEnumerable<MessageHeader> headers = null) =>
            GetRawMessages(message, headers)
                .ForEachAsync(async rawMessage =>
                    await ExecutePipeline(_behaviors, rawMessage, async x => 
                        x.Offset = await ProduceAsync(x)));

        private IEnumerable<RawBrokerMessage> GetRawMessages(object content, IEnumerable<MessageHeader> headers)
        {
            var headersCollection = new MessageHeaderCollection(headers);
            _messageKeyProvider.EnsureKeyIsInitialized(content, headersCollection);
            var rawMessage = new RawBrokerMessage(content, headersCollection, Endpoint);

            return ChunkProducer.ChunkIfNeeded(rawMessage);
        }

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        private async Task ExecutePipeline(IEnumerable<IProducerBehavior> behaviors, RawBrokerMessage message, RawBrokerMessageHandler finalAction)
        {
            if (behaviors != null && behaviors.Any())
            {
                await behaviors.First().Handle(message, m => ExecutePipeline(behaviors.Skip(1), m, finalAction));
            }
            else
            {
                await finalAction(message);
                _messageLogger.LogInformation(_logger, "Message produced.", message);
            }
        }

        protected abstract IOffset Produce(RawBrokerMessage message);

        protected abstract Task<IOffset> ProduceAsync(RawBrokerMessage message);
    }

    public abstract class Producer<TBroker, TEndpoint> : Producer
        where TBroker : class, IBroker
        where TEndpoint : class, IEndpoint
    {
        protected Producer(
            IBroker broker, 
            IEndpoint endpoint, 
            MessageKeyProvider messageKeyProvider,
            IEnumerable<IProducerBehavior> behaviors,
            ILogger<Producer> logger, 
            MessageLogger messageLogger)
            : base(broker, endpoint, messageKeyProvider, behaviors, logger, messageLogger)
        {
        }

        protected new TBroker Broker => (TBroker)base.Broker;

        protected new TEndpoint Endpoint => (TEndpoint)base.Endpoint;
    }
}