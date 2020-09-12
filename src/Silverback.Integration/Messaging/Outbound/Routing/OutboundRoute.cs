// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Connectors
{
    /// <inheritdoc cref="IOutboundRoute" />
    public class OutboundRoute : IOutboundRoute
    {
        private readonly Func<IServiceProvider, IOutboundRouter> _outboundRouterFactory;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OutboundRoute" /> class.
        /// </summary>
        /// <param name="messageType">
        ///     The type of the messages to be routed to the outbound endpoint(s).
        /// </param>
        /// <param name="outboundRouterFactory">
        ///     The factory to be used to resolve the <see cref="IOutboundRouter" /> to be used to determine the
        ///     destination endpoint.
        /// </param>
        /// <param name="outboundConnectorType">
        ///     The type of the <see cref="IOutboundConnector" /> to be used when publishing these messages.
        /// </param>
        public OutboundRoute(
            Type messageType,
            Func<IServiceProvider, IOutboundRouter> outboundRouterFactory,
            Type? outboundConnectorType)
        {
            MessageType = messageType;
            _outboundRouterFactory = outboundRouterFactory;
            OutboundConnectorType = outboundConnectorType;
        }

        /// <inheritdoc cref="IOutboundRoute.MessageType" />
        public Type MessageType { get; }

        /// <inheritdoc cref="IOutboundRoute.OutboundConnectorType" />
        public Type? OutboundConnectorType { get; }

        /// <inheritdoc cref="IOutboundRoute.GetOutboundRouter" />
        public IOutboundRouter GetOutboundRouter(IServiceProvider serviceProvider) =>
            _outboundRouterFactory.Invoke(serviceProvider);
    }
}
