// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Outbound.Routing
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
        public OutboundRoute(
            Type messageType,
            Func<IServiceProvider, IOutboundRouter> outboundRouterFactory)
        {
            MessageType = messageType;
            _outboundRouterFactory = outboundRouterFactory;
        }

        /// <inheritdoc cref="IOutboundRoute.MessageType" />
        public Type MessageType { get; }

        /// <inheritdoc cref="IOutboundRoute.GetOutboundRouter" />
        public IOutboundRouter GetOutboundRouter(IServiceProvider serviceProvider) =>
            _outboundRouterFactory.Invoke(serviceProvider);
    }
}
