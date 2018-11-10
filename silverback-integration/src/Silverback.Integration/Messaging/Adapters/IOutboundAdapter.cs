using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Adapters
{
    /// <summary>
    /// An adapter that publishes the outgoing messages to the message broker.
    /// </summary>
    public interface IOutboundAdapter
    {
        void Relay(IIntegrationMessage message, IProducer producer, IEndpoint endpoint);

        Task RelayAsync(IIntegrationMessage message, IProducer producer, IEndpoint endpoint);
    }
}