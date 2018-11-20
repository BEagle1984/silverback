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
            _logger.LogTrace($"Producing message {message.GetTraceString(Endpoint)}.");
            Produce(message, Endpoint.Serializer.Serialize(message));
        }

        public async Task ProduceAsync(IMessage message)
        {
            _logger.LogTrace($"Producing message {message.GetTraceString(Endpoint)}.");
            await ProduceAsync(message, Endpoint.Serializer.Serialize(message));
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