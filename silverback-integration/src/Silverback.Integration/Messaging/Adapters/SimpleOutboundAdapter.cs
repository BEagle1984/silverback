using System;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Adapters
{
    /// <summary>
    /// The basic outbound adapter that sends the messages directly through the message broker.
    /// </summary>
    /// <seealso cref="Silverback.Messaging.Adapters.IOutboundAdapter" />
    /// <seealso cref="System.IDisposable" />
    public class SimpleOutboundAdapter : IOutboundAdapter
    {
        /// <summary>
        /// Publishes the <see cref="T:Silverback.Messaging.Messages.IIntegrationMessage" /> to the specified <see cref="T:Silverback.Messaging.IEndpoint" />.
        /// </summary>
        /// <param name="message">The message to be handled.</param>
        /// <param name="endpoint">The endpoint.</param>
        public void Relay(IIntegrationMessage message, IEndpoint endpoint)
            => endpoint.GetProducer().Produce(Envelope.Create(message));
    }
}