using System;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Adapters
{
    /// <summary>
    /// The basic outbound adapter that sends the messages directly through the message broker.
    /// </summary>
    /// <seealso cref="Silverback.Messaging.Adapters.IOutboundAdapter" />
    public class OutboundAdapter : IOutboundAdapter
    {
        public void Relay(IIntegrationMessage message, IProducer producer, IEndpoint endpoint)
            => producer.Produce(Envelope.Create(message));

        public Task RelayAsync(IIntegrationMessage message, IProducer producer, IEndpoint endpoint)
            => producer.ProduceAsync(Envelope.Create(message));
    }
}