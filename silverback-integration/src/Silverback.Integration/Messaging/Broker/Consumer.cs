// Copyright (c) 2018 Sergio Aquilini
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

        protected Consumer(IBroker broker, IEndpoint endpoint, ILogger<Consumer> logger)
           : base(broker, endpoint)
        {
            _logger = logger;
        }

        public event ReceivedEventHandler Received;

        public void Acknowledge(object offset) => Acknowledge(new[] {offset});

        public abstract void Acknowledge(IEnumerable<object> offsets);

        protected void HandleMessage(byte[] buffer, object offset)
        {
            if (Received == null)
                throw new InvalidOperationException("A message was received but no handler is configured, please attach to the Received event.");

            var message = Endpoint.Serializer.Deserialize(buffer);

            _logger.LogTrace("Message received.", message, Endpoint);

            Received.Invoke(this, message, offset);
        }
    }

    public abstract class Consumer<TBroker, TEndpoint> : Consumer
        where TBroker : class, IBroker
        where TEndpoint : class, IEndpoint
    {
        protected Consumer(IBroker broker, IEndpoint endpoint, ILogger<Consumer> logger) 
            : base(broker, endpoint, logger)
        {
        }

        protected new TBroker Broker => (TBroker)base.Broker;

        protected new TEndpoint Endpoint => (TEndpoint)base.Endpoint;
    }
}