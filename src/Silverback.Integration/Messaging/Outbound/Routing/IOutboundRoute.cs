// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Outbound.Routing
{
    /// <summary>
    ///     A configured outbound route, defining the destination endpoint for a specific message type.
    /// </summary>
    public interface IOutboundRoute
    {
        /// <summary>
        ///     Gets the type of the messages to be routed.
        /// </summary>
        Type MessageType { get; }

        /// <summary>
        ///     Gets the producer configuration.
        /// </summary>
        ProducerConfiguration ProducerConfiguration { get; }
    }
}
