// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;

namespace Silverback.Messaging.Connectors
{
    public class OutboundRoutingConfiguration : IOutboundRoutingConfiguration
    {
        private readonly List<OutboundRoute> _routes = new List<OutboundRoute>();

        public bool PublishOutboundMessagesToInternalBus { get; set; }

        public IEnumerable<IOutboundRoute> Routes => _routes.AsReadOnly();

        public IOutboundRoutingConfiguration Add<TMessage>(IProducerEndpoint endpoint, Type outboundConnectorType) =>
            Add(typeof(TMessage), endpoint, outboundConnectorType);

        public IOutboundRoutingConfiguration Add(
            Type messageType,
            IProducerEndpoint endpoint,
            Type outboundConnectorType)
        {
            _routes.Add(new OutboundRoute(messageType, endpoint, outboundConnectorType));
            return this;
        }

        public IEnumerable<IOutboundRoute> GetRoutesForMessage(object message) =>
            _routes.Where(r => r.MessageType.IsInstanceOfType(message)).ToList();

        public class OutboundRoute : IOutboundRoute
        {
            public OutboundRoute(Type messageType, IProducerEndpoint destinationEndpoint, Type outboundConnectorType)
            {
                MessageType = messageType;
                DestinationEndpoint = destinationEndpoint;
                OutboundConnectorType = outboundConnectorType;
            }

            public Type MessageType { get; }
            public IProducerEndpoint DestinationEndpoint { get; }
            public Type OutboundConnectorType { get; }
        }
    }
}