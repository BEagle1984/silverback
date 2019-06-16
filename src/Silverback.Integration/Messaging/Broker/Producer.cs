// Copyright (c) 2018-2019 Sergio Aquilini
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
            GetMessageContentChunks(message)
                .ForEach(x =>
                {
                    var offset = Produce(x.message, x.serializedMessage, headers);
                    Trace(message, offset);
                });

        public Task ProduceAsync(object message, IEnumerable<MessageHeader> headers = null) =>
            GetMessageContentChunks(message)
                .ForEachAsync(async x =>
                {
                    var offset = await ProduceAsync(x.message, x.serializedMessage, headers);
                    Trace(message, offset);
                });

        private IEnumerable<(object message, byte[] serializedMessage)> GetMessageContentChunks(object message)
        {
            _messageKeyProvider.EnsureKeyIsInitialized(message);

            return ChunkProducer.ChunkIfNeeded(
                _messageKeyProvider.GetKey(message, false),
                message,
                (Endpoint as IProducerEndpoint)?.Chunk,
                Endpoint.Serializer);
        }

        private void Trace(object message, IOffset offset) =>
            _messageLogger.LogInformation(_logger, "Message produced.", message, Endpoint, offset);

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