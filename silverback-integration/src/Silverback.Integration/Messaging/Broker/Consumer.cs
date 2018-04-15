using System;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    /// The default <see cref="IConsumer"/> implementation.
    /// </summary>
    public abstract class Consumer : ConsumerProducerBase, IConsumer
    {
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

                var envelope = Serializer.Deserialize(buffer);
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