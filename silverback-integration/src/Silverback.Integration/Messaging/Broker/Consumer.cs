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
        /// Occurs when a message is received.
        /// </summary>
        public event EventHandler<IEnvelope> Received;

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
        /// Starts consuming.
        /// </summary>
        public void Start()
        {
            // TODO: Handle errors -> logging and stuff -> then?
            StartConsuming(buffer =>
            {
                if (Received == null)
                    return;

                var envelope = _serializer.Deserialize(buffer);
                Received(this, envelope);
            });
        }

        /// <summary>
        /// Stops consuming.
        /// </summary>
        public void Stop()
            => StopConsuming();

        /// <summary>
        /// Starts consuming messages from the related enpoint and call the specified handler when a message
        /// is received.
        /// </summary>
        /// <param name="handler">The message handler.</param>
        protected abstract void StartConsuming(Action<byte[]> handler);

        /// <summary>
        /// Stops consuming messages.
        /// </summary>
        protected abstract void StopConsuming();
    }
}