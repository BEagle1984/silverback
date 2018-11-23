using System;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Broker
{
    public abstract class Consumer : EndpointConnectedObject, IConsumer
    {
        private readonly ILogger<Consumer> _logger;

        public event EventHandler<IMessage> Received;

        protected Consumer(IBroker broker, IEndpoint endpoint, ILogger<Consumer> logger)
           : base(broker, endpoint)
        {
            _logger = logger;
        }

        protected void HandleMessage(byte[] buffer)
        {
            if (Received == null)
                throw new InvalidOperationException("A message was received but no handler is attached to the Received event.");

            var message = DeserializeMessage(buffer);

            _logger.LogTrace($"Received message {message.GetTraceString(Endpoint)}.");

            try
            {
                Received(this, message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    $"Error occurred processing the message {message.GetTraceString(Endpoint)}.");

                throw;
            }
        }

        private IMessage DeserializeMessage(byte[] buffer)
        {
            try
            {
                return Endpoint.Serializer.Deserialize(buffer);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error occurred deserializing a message.");
                throw;
            }
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