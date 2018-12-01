using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

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

        public void Produce(IMessage message)
        {
            EnsureIdentifier(message);
            Trace(message);
            Produce(message, Endpoint.Serializer.Serialize(message));
        }

        public async Task ProduceAsync(IMessage message)
        {
            EnsureIdentifier(message);
            Trace(message);
            await ProduceAsync(message, Endpoint.Serializer.Serialize(message));
        }

        private void Trace(IMessage message) =>
            _logger.LogTrace("Producing message.", message, Endpoint);

        private void EnsureIdentifier(IMessage message)
        {
            if (message is IIntegrationMessage integrationMessage)
                integrationMessage.Id = Guid.NewGuid();
        }

        protected abstract void Produce(IMessage message, byte[] serializedMessage);

        protected abstract Task ProduceAsync(IMessage message, byte[] serializedMessage);
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