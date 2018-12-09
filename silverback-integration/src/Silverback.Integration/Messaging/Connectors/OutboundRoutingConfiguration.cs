// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors
{
    public class OutboundRoutingConfiguration : IOutboundRoutingConfiguration // TODO: Unit test this?
    {
        private readonly List<OutboundRoute> _routes = new List<OutboundRoute>();

        public IReadOnlyCollection<OutboundRoute> Routes => _routes.AsReadOnly();

        public IOutboundRoutingConfiguration Add<TMessage>(IEndpoint endpoint, Type outboundConnectorType) where TMessage : IIntegrationMessage
        {
            _routes.Add(new OutboundRoute(typeof(TMessage), endpoint, outboundConnectorType));
            return this;
        }

        public IEnumerable<IOutboundRoute> GetRoutes(IIntegrationMessage message) =>
            _routes.Where(r => r.MessageType.IsInstanceOfType(message)).ToList();

        public class OutboundRoute : IOutboundRoute
        {
            public OutboundRoute(Type messageType, IEndpoint destinationEndpoint, Type outboundConnectorType)
            {
                MessageType = messageType;
                DestinationEndpoint = destinationEndpoint;
                OutboundConnectorType = outboundConnectorType;
            }

            public Type MessageType { get; }
            public IEndpoint DestinationEndpoint { get; }
            public Type OutboundConnectorType { get; }
        }
    }
}