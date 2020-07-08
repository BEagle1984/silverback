// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;

namespace Silverback.Messaging.Connectors
{
    /// <inheritdoc />
    public class OutboundRoutingConfiguration : IOutboundRoutingConfiguration
    {
        private readonly List<OutboundRoute> _routes = new List<OutboundRoute>();

        public bool PublishOutboundMessagesToInternalBus { get; set; }

        public IEnumerable<IOutboundRoute> Routes => _routes.AsReadOnly();

        public IOutboundRoutingConfiguration Add<TMessage>(Func<IServiceProvider, IOutboundRouter> outboundRouterFactory, Type outboundConnectorType) =>
            Add(typeof(TMessage), outboundRouterFactory, outboundConnectorType);

        public IOutboundRoutingConfiguration Add(
            Type messageType,
            Func<IServiceProvider, IOutboundRouter> outboundRouterFactory,
            Type outboundConnectorType = null)
        {
            _routes.Add(new OutboundRoute(messageType, outboundRouterFactory, outboundConnectorType));
            return this;
        }

        public IEnumerable<IOutboundRoute> GetRoutesForMessage(object message) =>
            _routes.Where(r => r.MessageType.IsInstanceOfType(message)).ToList();
    }
}