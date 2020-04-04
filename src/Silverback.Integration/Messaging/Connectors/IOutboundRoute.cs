// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    ///     A configured outbound route for the specified type of message.
    /// </summary>
    public interface IOutboundRoute
    {
        /// <summary>
        ///     Gets the type of the messages to be routed to the outbound endpoint(s).
        /// </summary>
        Type MessageType { get; }

        /// <summary>
        ///     Gets the instance of <see cref="IOutboundRouter" /> to be used to determine the destination endpoint.
        /// </summary>
        IOutboundRouter Router { get; }

        /// <summary>
        ///     Gets the type of the <see cref="IOutboundConnector" /> to be used when publishing these messages.
        /// </summary>
        Type OutboundConnectorType { get; }
    }
}