using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Util;

namespace Silverback.Messaging.Connectors
{
    public abstract class OutboundConnectorBase : ISubscriber
    {
        private readonly IOutboundRoutingConfiguration _routing;

        protected OutboundConnectorBase(IOutboundRoutingConfiguration routingConfiguration)
        {
            _routing = routingConfiguration;
        }

        [Subscribe]
        public Task OnMessageReceived(IIntegrationMessage message) =>
            _routing.GetDestinations(message)
                .ForEachAsync(endpoint => RelayMessage(message, endpoint));

        protected abstract Task RelayMessage(IIntegrationMessage message, IEndpoint destinationEndpoint);
    }
}