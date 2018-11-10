using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Integration
{
    /// <summary>
    /// Connecto to a message broker to relay the outgoing integration messages.
    /// </summary>
    public interface IOutboundConnector
    {
        void Relay(IIntegrationMessage message, IProducer producer, IEndpoint endpoint);

        Task RelayAsync(IIntegrationMessage message, IProducer producer, IEndpoint endpoint);
    }
}