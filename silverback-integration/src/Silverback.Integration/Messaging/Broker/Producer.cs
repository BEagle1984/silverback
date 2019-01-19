// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public abstract class Producer : EndpointConnectedObject, IProducer
    {
        private readonly ILogger<Producer> _logger;

        protected Producer(IBroker broker, IEndpoint endpoint, ILogger<Producer> logger)
            : base(broker, endpoint)
        {
            _logger = logger;
        }

        public void Produce(object message)
        {
            EnsureIdentifier(message);
            Trace(message);
            Produce(message, Endpoint.Serializer.Serialize(message));
        }

        public async Task ProduceAsync(object message)
        {
            EnsureIdentifier(message);
            Trace(message);
            await ProduceAsync(message, Endpoint.Serializer.Serialize(message));
        }

        private void Trace(object message) =>
            _logger.LogMessageTrace("Producing message.", message, Endpoint);

        private void EnsureIdentifier(object message)
        {
            if (message is IIntegrationMessage integrationMessage)
                integrationMessage.Id = Guid.NewGuid();
        }

        protected abstract void Produce(object message, byte[] serializedMessage);

        protected abstract Task ProduceAsync(object message, byte[] serializedMessage);
    }

    public abstract class Producer<TBroker, TEndpoint> : Producer
        where TBroker : class, IBroker
        where TEndpoint : class, IEndpoint
    {
        protected Producer(IBroker broker, IEndpoint endpoint, ILogger<Producer> logger)
            : base(broker, endpoint, logger)
        {
        }

        protected new TBroker Broker => (TBroker)base.Broker;

        protected new TEndpoint Endpoint => (TEndpoint)base.Endpoint;
    }
}