// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
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
        private readonly MessageLogger _messageLogger;
        private readonly ILogger<Producer> _logger;

        protected Producer(IBroker broker, IEndpoint endpoint, MessageKeyProvider messageKeyProvider,
            ILogger<Producer> logger, MessageLogger messageLogger)
            : base(broker, endpoint)
        {
            _messageKeyProvider = messageKeyProvider;
            _logger = logger;
            _messageLogger = messageLogger;
        }

        public void Produce(object message, IEnumerable<MessageHeader> headers = null) =>
            GetOutboundMessages(message, headers)
                .ForEach(x =>
                {
                    x.Offset = Produce(x.Content, x.RawContent, x.Headers);
                    Trace(x);
                });

        public Task ProduceAsync(object message, IEnumerable<MessageHeader> headers = null) =>
            GetOutboundMessages(message, headers)
                .ForEachAsync(async x =>
                {
                    x.Offset = await ProduceAsync(x.Content, x.RawContent, x.Headers);
                    Trace(x);
                });

        private IEnumerable<OutboundMessage> GetOutboundMessages(object message, IEnumerable<MessageHeader> headers)
        {
            var outboundMessage = new OutboundMessage(message, headers, Endpoint);

            _messageKeyProvider.EnsureKeyIsInitialized(outboundMessage);

            outboundMessage.RawContent = Endpoint.Serializer
                .Serialize(outboundMessage.Content, outboundMessage.Headers);

            return ChunkProducer.ChunkIfNeeded(outboundMessage);
        }

        private void Trace(IOutboundMessage message) => 
            _messageLogger.LogInformation(_logger, "Message produced.", message);

        protected abstract IOffset Produce(object message, byte[] serializedMessage, IEnumerable<MessageHeader> headers);

        protected abstract Task<IOffset> ProduceAsync(object message, byte[] serializedMessage, IEnumerable<MessageHeader> headers);
    }

    public abstract class Producer<TBroker, TEndpoint> : Producer
        where TBroker : class, IBroker
        where TEndpoint : class, IEndpoint
    {
        protected Producer(IBroker broker, IEndpoint endpoint, MessageKeyProvider messageKeyProvider,
            ILogger<Producer> logger, MessageLogger messageLogger) 
            : base(broker, endpoint, messageKeyProvider, logger, messageLogger)
        {
        }

        protected new TBroker Broker => (TBroker)base.Broker;

        protected new TEndpoint Endpoint => (TEndpoint)base.Endpoint;
    }
}