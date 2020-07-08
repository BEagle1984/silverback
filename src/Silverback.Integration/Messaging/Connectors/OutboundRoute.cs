// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Connectors
{
    /// <inheritdoc cref="IOutboundRoute" />
    public class OutboundRoute : IOutboundRoute
    {
        private readonly Func<IServiceProvider, IOutboundRouter> _outboundRouterFactory;

        public OutboundRoute(Type messageType, Func<IServiceProvider, IOutboundRouter> outboundRouterFactory, Type outboundConnectorType)
        {
            MessageType = messageType;
            _outboundRouterFactory = outboundRouterFactory;
            OutboundConnectorType = outboundConnectorType;
        }

        public Type MessageType { get; }

        public Type OutboundConnectorType { get; }

        public IOutboundRouter GetOutboundRouter(IServiceProvider serviceProvider) =>
            _outboundRouterFactory.Invoke(serviceProvider);
    }
}