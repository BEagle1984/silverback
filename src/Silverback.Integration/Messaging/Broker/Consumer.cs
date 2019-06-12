// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public abstract class Consumer : EndpointConnectedObject, IConsumer
    {
        private readonly ILogger<Consumer> _logger;
        private readonly MessageLogger _messageLogger;

        protected Consumer(IBroker broker, IEndpoint endpoint,ILogger<Consumer> logger, MessageLogger messageLogger)
           : base(broker, endpoint)
        {
            _logger = logger;
            _messageLogger = messageLogger;
        }

        public event EventHandler<MessageReceivedEventArgs> Received;

        public void Acknowledge(IOffset offset)
        {
            Acknowledge(new[] {offset});
        }

        public abstract void Acknowledge(IEnumerable<IOffset> offsets);

        protected void HandleMessage(byte[] message, IEnumerable<MessageHeader> headers, IOffset offset)
        {
            if (Received == null)
                throw new InvalidOperationException("A message was received but no handler is configured, please attach to the Received event.");

            _messageLogger.LogTrace(_logger, "Message received.", null, Endpoint, offset);

            Received.Invoke(this, new MessageReceivedEventArgs(message, headers, offset));
        }
    }

    public abstract class Consumer<TBroker, TEndpoint> : Consumer
        where TBroker : class, IBroker
        where TEndpoint : class, IEndpoint
    {
        protected Consumer(IBroker broker, IEndpoint endpoint,
            ILogger<Consumer> logger, MessageLogger messageLogger)
            : base(broker, endpoint, logger, messageLogger)
        {
        }

        protected new TBroker Broker => (TBroker)base.Broker;

        protected new TEndpoint Endpoint => (TEndpoint)base.Endpoint;
    }
}