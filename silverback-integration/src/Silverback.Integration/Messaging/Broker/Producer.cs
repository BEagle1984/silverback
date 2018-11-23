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

        public void Produce(IEnvelope envelope)
        {
            _logger.LogTrace($"Producing message '{envelope.Message.Id}' to endpoint '{Endpoint.Name}'.");
            Produce(envelope.Message, Serializer.Serialize(envelope));
        }

        public async Task ProduceAsync(IEnvelope envelope)
        {
            _logger.LogTrace($"Producing message '{envelope.Message.Id}' to endpoint '{Endpoint.Name}'.");
            await ProduceAsync(envelope.Message, Serializer.Serialize(envelope));
        }

        protected abstract void Produce(IIntegrationMessage message, byte[] serializedMessage);

        protected abstract Task ProduceAsync(IIntegrationMessage message, byte[] serializedMessage);
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