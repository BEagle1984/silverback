// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;

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

        public void Produce(object message)
        {
            _messageKeyProvider.EnsureKeyIsInitialized(message);
            Trace(message);
            Produce(message, Endpoint.Serializer.Serialize(message));
        }

        public async Task ProduceAsync(object message)
        {
            _messageKeyProvider.EnsureKeyIsInitialized(message);
            Trace(message);
            await ProduceAsync(message, Endpoint.Serializer.Serialize(message));
        }

        private void Trace(object message) =>
            _messageLogger.LogTrace(_logger, "Producing message.", message, Endpoint);

        protected abstract void Produce(object message, byte[] serializedMessage);

        protected abstract Task ProduceAsync(object message, byte[] serializedMessage);
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