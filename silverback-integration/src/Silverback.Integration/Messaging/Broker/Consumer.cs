using System;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    /// The default <see cref="IConsumer"/> implementation.
    /// </summary>
    public abstract class Consumer : IConsumer
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
        protected Consumer(IEndpoint endpoint)
        {
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _serializer = endpoint.GetBroker().GetSerializer();
        }

        /// <summary>
        /// Start listening to the specified enpoint and consume the messages delivered
        /// through the message broker.
        /// </summary>
        /// <param name="handler">The message handler.</param>
        public void Consume(Action<IEnvelope> handler)
        {
            // TODO: Handle errors -> logging and stuff -> then?
            Consume(buffer =>
            {
                var envelope = _serializer.Deserialize(buffer);
                handler(envelope);
            });
        }

        /// <summary>
        /// Start listening to the specified enpoint and consume the messages delivered
        /// through the message broker.
        /// </summary>
        /// <param name="handler">The message handler.</param>
        protected abstract void Consume(Action<byte[]> handler);
    }
}