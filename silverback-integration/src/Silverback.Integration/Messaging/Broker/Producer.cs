using System;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    /// The default <see cref="IProducer"/> implementation.
    /// </summary>
    /// <seealso cref="Silverback.Messaging.Broker.IProducer" />
    public abstract class Producer : IProducer
    {
        private readonly IMessageSerializer _serializer;

        /// <summary>
        /// Gets the endpoint.
        /// </summary>
        public IEndpoint Endpoint { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Consumer" /> class.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        protected Producer(IEndpoint endpoint)
        {
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _serializer = Endpoint.GetBroker().GetSerializer();
        }

        /// <summary>
        /// Sends the specified message through the message broker.
        /// </summary>
        /// <param name="envelope">The envelope containing the message to be sent.</param>
        public void Produce(IEnvelope envelope)
        {
            Produce(envelope.Message, _serializer.Serialize(envelope));
        }

        /// <summary>
        /// Sends the specified message through the message broker.
        /// </summary>
        /// <param name="message">The original message.</param>
        /// <param name="serializedMessage">The serialized <see cref="IEnvelope"/> including the <see cref="IIntegrationMessage"/>.
        /// This is what is supposed to be sent through the broker.</param>
        protected abstract void Produce(IIntegrationMessage message, byte[] serializedMessage);
    }
}