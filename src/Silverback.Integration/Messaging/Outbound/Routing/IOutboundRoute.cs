// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Outbound.Routing
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
        ///     Gets the type of the <see cref="IOutboundConnector" /> to be used when publishing these messages. If
        ///     not specified, the default one will be used.
        /// </summary>
        Type? OutboundConnectorType { get; }

        /// <summary>
        ///     Returns the instance of <see cref="IOutboundRouter" /> to be used to determine the destination
        ///     endpoint.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider" /> to be used to resolve the router.</param>
        /// <returns>The instance of <see cref="IOutboundRouter{TMessage}" />.</returns>
        IOutboundRouter GetOutboundRouter(IServiceProvider serviceProvider);
    }
}
