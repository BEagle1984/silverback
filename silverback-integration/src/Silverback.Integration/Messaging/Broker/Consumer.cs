// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public abstract class Consumer : EndpointConnectedObject, IConsumer
    {
        private readonly ChunkConsumer _chunkConsumer;
        private readonly ILogger<Consumer> _logger;
        private readonly MessageLogger _messageLogger;

        protected Consumer(IBroker broker, IEndpoint endpoint, ChunkConsumer chunkConsumer, ILogger<Consumer> logger, MessageLogger messageLogger)
           : base(broker, endpoint)
        {
            _chunkConsumer = chunkConsumer;
            _logger = logger;
            _messageLogger = messageLogger;
        }

        public event EventHandler<MessageReceivedEventArgs> Received;

        public void Acknowledge(IOffset offset) => Acknowledge(new[] { offset });

        public abstract void Acknowledge(IEnumerable<IOffset> offsets);

        protected void HandleMessage(byte[] message, IOffset offset)
        {
            if (Received == null)
                throw new InvalidOperationException("A message was received but no handler is configured, please attach to the Received event.");

            var deserializedMessage = Endpoint.Serializer.Deserialize(message);

            if (deserializedMessage is MessageChunk chunk)
            {
                var joined = _chunkConsumer.JoinIfComplete(chunk);

                if (joined == null)
                    return;

                deserializedMessage = Endpoint.Serializer.Deserialize(joined);
            }

            _messageLogger.LogTrace(_logger, "Message received.", deserializedMessage, Endpoint);

            Received.Invoke(this, new MessageReceivedEventArgs(deserializedMessage, offset));
        }
    }

    public abstract class Consumer<TBroker, TEndpoint> : Consumer
        where TBroker : class, IBroker
        where TEndpoint : class, IEndpoint
    {
        protected Consumer(IBroker broker, IEndpoint endpoint, ChunkConsumer chunkConsumer,
            ILogger<Consumer> logger, MessageLogger messageLogger)
            : base(broker, endpoint, chunkConsumer, logger, messageLogger)
        {
        }

        protected new TBroker Broker => (TBroker)base.Broker;

        protected new TEndpoint Endpoint => (TEndpoint)base.Endpoint;
    }
}