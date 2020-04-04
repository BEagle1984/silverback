// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Connectors
{
    /// <inheritdoc cref="IOutboundRoute" />
    public class OutboundRoute : IOutboundRoute
    {
        public OutboundRoute(Type messageType, IOutboundRouter router, Type outboundConnectorType)
        {
            MessageType = messageType;
            Router = router;
            OutboundConnectorType = outboundConnectorType;
        }

        /// <inheritdoc cref="IOutboundRoute" />
        public Type MessageType { get; }

        /// <inheritdoc cref="IOutboundRoute" />
        public IOutboundRouter Router { get; }

        /// <inheritdoc cref="IOutboundRoute" />
        public Type OutboundConnectorType { get; }
    }
}