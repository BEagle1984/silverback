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
        /// Occurs when a message is received.
        /// </summary>
        event EventHandler<IEnvelope> Received;
    }
}