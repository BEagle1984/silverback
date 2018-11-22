using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Util;

namespace Silverback.Messaging.Connectors
{
    public class OutboundConnectorRouter : ISubscriber
    {
        private readonly IOutboundRoutingConfiguration _routing;
        private readonly IEnumerable<IOutboundConnector> _outboundConnectors;

        public OutboundConnectorRouter(IOutboundRoutingConfiguration routingConfiguration, IEnumerable<IOutboundConnector> outboundConnectors)
        {
            _routing = routingConfiguration;
            _outboundConnectors = outboundConnectors;
        }

        [Subscribe]
        public Task OnMessageReceived(IIntegrationMessage message) =>
            _routing.GetRoutes(message)
                .ForEachAsync(route =>
                    GetConnectorInstance(route).RelayMessage(message, route.DestinationEndpoint));

        private IOutboundConnector GetConnectorInstance(IOutboundRoute route)
        {
            IOutboundConnector connector;

            if (route.OutboundConnectorType == null)
            {
                connector = _outboundConnectors.FirstOrDefault();
            }
            else
            {
                connector = _outboundConnectors.FirstOrDefault(c => c.GetType() == route.OutboundConnectorType)
                      ?? _outboundConnectors.FirstOrDefault(c => route.OutboundConnectorType.IsInstanceOfType(c));
            }

            if (connector == null)
                throw new SilverbackException($"No instance of {route.OutboundConnectorType?.Name ?? "IOutboundConnector"} was resolved.");

            return connector;
        }
    }
}