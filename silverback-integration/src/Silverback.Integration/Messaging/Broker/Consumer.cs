using System;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    /// The default <see cref="IConsumer" /> implementation.
    /// </summary>
    /// <seealso cref="Silverback.Messaging.Broker.EndpointConnectedObject" />
    /// <seealso cref="Silverback.Messaging.Broker.IConsumer" />
    public abstract class Consumer : EndpointConnectedObject, IConsumer
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Occurs when a message is received.
        /// </summary>
        public event EventHandler<IEnvelope> Received;

        /// <summary>
        /// Initializes a new instance of the <see cref="Consumer" /> class.
        /// </summary>
        /// <param name="broker">The broker.</param>
        /// <param name="endpoint">The endpoint.</param>
        protected Consumer(IBroker broker, IEndpoint endpoint)
           : base(broker, endpoint)
        {
            _logger = broker.LoggerFactory.CreateLogger<Consumer>();
        }

        /// <summary>
        /// Handles the received message firing the Received event.
        /// </summary>
        /// <remarks>In a derived class use this method to deserialize the message and fire 
        /// the event.</remarks>
        protected void HandleMessage(byte[] buffer)
        {
            if (Received == null)
                throw new InvalidOperationException("A message was received but no handler is attached to the Received event.");

            var envelope = Serializer.Deserialize(buffer);

            _logger.LogTrace($"Received message '{envelope.Message.Id}' on endpoint '{Endpoint.Name}' (source: '{envelope.Source}').");

            Received(this, envelope);
        }
    }
}