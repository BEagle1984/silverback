using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    /// Holds the outbound messages routing configuration (which message is redirected to which endpoint).
    /// </summary>
    public interface IOutboundRoutingConfiguration
    {
        IOutboundRoutingConfiguration Add<TMessage>(IEndpoint endpoint) where TMessage : IIntegrationMessage;

        IEnumerable<IEndpoint> GetDestinations(IIntegrationMessage message);
    }
}