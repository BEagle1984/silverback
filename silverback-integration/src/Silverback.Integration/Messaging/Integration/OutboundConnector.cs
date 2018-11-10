using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Integration
{
    /// <summary>
    /// The basic outbound connector that sends the messages directly through the message broker.
    /// </summary>
    /// <seealso cref="IOutboundConnector" />
    public class OutboundConnector : IOutboundConnector
    {
        public void Relay(IIntegrationMessage message, IProducer producer, IEndpoint endpoint)
            => producer.Produce(Envelope.Create(message));

        public Task RelayAsync(IIntegrationMessage message, IProducer producer, IEndpoint endpoint)
            => producer.ProduceAsync(Envelope.Create(message));
    }
}