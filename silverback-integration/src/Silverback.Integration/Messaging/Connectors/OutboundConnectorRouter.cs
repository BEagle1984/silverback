// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
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
                    _outboundConnectors.GetConnectorInstance(route.OutboundConnectorType)
                .RelayMessage(message, route.DestinationEndpoint));
    }
}