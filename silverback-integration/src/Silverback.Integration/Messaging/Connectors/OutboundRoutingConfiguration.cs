using System;
using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors
{
    public class OutboundRoutingConfiguration : IOutboundRoutingConfiguration // TODO: Unit test this?
    {
        private readonly List<Route> _routes = new List<Route>();

        public IOutboundRoutingConfiguration Add<TMessage>(IEndpoint endpoint) where TMessage : IIntegrationMessage
        {
            _routes.Add(new Route(typeof(TMessage), endpoint));
            return this;
        }

        public IReadOnlyCollection<Route> Routes => _routes.AsReadOnly();

        public IEnumerable<IEndpoint> GetDestinations(IIntegrationMessage message) =>
            _routes
                .Where(r => r.MessageType.IsInstanceOfType(message))
                .Select(r => r.DestinationEndpoint);

        public class Route
        {
            public Route(Type messageType, IEndpoint destinationEndpoint)
            {
                MessageType = messageType;
                DestinationEndpoint = destinationEndpoint;
            }

            public Type MessageType { get; }
            public IEndpoint DestinationEndpoint { get; }
        }
    }
}