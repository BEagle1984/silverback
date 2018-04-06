using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    /// Exposes the methods to receive messages through the message broker.
    /// </summary>
    public interface IConsumer
    {
        /// <summary>
        /// Start listening to the specified enpoint and consume the messages delivered
        /// through the message broker.
        /// </summary>
        /// <param name="handler">The handler.</param>
        void Consume(Action<IEnvelope> handler);

    }
}