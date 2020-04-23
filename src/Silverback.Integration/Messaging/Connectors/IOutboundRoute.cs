// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    ///     Defines the <see cref="IOutboundRouter" /> to be used to get the destination endpoints to produce
    ///     the messages of the specified type to.
    /// </summary>
    public interface IOutboundRoute
    {
        /// <summary>
        ///     Gets the type of the messages to be routed to the outbound endpoint(s).
        /// </summary>
        Type MessageType { get; }

        /// <summary>
        ///     Gets the <see cref="IOutboundRouter" /> to be used to determine the destination endpoint.
        /// </summary>
        IOutboundRouter Router { get; }

        /// <summary>
        ///     Gets the type of the <see cref="IOutboundConnector" /> to be used when publishing these messages. If
        ///     not specified, the default one will be used.
        /// </summary>
        Type? OutboundConnectorType { get; }
    }
}
