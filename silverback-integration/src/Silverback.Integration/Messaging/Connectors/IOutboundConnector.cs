using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    /// Subscribes to the internal bus and forwards the integration messages to the message broker.
    /// </summary>
    public interface IOutboundConnector
    {
        Task RelayMessage(IIntegrationMessage message, IEndpoint destinationEndpoint);
    }
}