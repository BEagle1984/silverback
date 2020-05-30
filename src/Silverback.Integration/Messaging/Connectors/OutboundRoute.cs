// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Connectors
{
    /// <inheritdoc cref="IOutboundRoute" />
    public class OutboundRoute : IOutboundRoute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="OutboundRoute" /> class.
        /// </summary>
        /// <param name="messageType">
        ///     The type of the messages to be routed to the outbound endpoint(s).
        /// </param>
        /// <param name="router">
        ///     The <see cref="IOutboundRouter" /> to be used to determine the destination endpoint.
        /// </param>
        /// <param name="outboundConnectorType">
        ///     The type of the <see cref="IOutboundConnector" /> to be used when publishing these messages.
        /// </param>
        public OutboundRoute(Type messageType, IOutboundRouter router, Type? outboundConnectorType)
        {
            MessageType = messageType;
            Router = router;
            OutboundConnectorType = outboundConnectorType;
        }

        /// <inheritdoc cref="IOutboundRoute.MessageType" />
        public Type MessageType { get; }

        /// <inheritdoc cref="IOutboundRoute.Router" />
        public IOutboundRouter Router { get; }

        /// <inheritdoc cref="IOutboundRoute.OutboundConnectorType" />
        public Type? OutboundConnectorType { get; }
    }
}
