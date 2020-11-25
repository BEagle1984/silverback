// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;

namespace Silverback.Messaging.Outbound.Routing
{
    internal class OutboundRoutingConfiguration : IOutboundRoutingConfiguration
    {
        private readonly List<OutboundRoute> _routes = new List<OutboundRoute>();

        public IReadOnlyCollection<IOutboundRoute> Routes => _routes.AsReadOnly();

        public bool PublishOutboundMessagesToInternalBus { get; set; }

        public IOutboundRoutingConfiguration Add<TMessage>(
            Func<IServiceProvider, IOutboundRouter> outboundRouterFactory) =>
            Add(typeof(TMessage), outboundRouterFactory);

        public IOutboundRoutingConfiguration Add(
            Type messageType,
            Func<IServiceProvider, IOutboundRouter> outboundRouterFactory)
        {
            _routes.Add(new OutboundRoute(messageType, outboundRouterFactory));
            return this;
        }

        public IReadOnlyCollection<IOutboundRoute> GetRoutesForMessage(object message) =>
            GetRoutesForMessage(message.GetType());

        public IReadOnlyCollection<IOutboundRoute> GetRoutesForMessage(Type messageType) =>
            _routes.Where(r => r.MessageType.IsAssignableFrom(messageType)).ToList();
    }
}
