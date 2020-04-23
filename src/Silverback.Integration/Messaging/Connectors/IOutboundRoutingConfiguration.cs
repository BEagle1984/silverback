// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    ///     Holds the outbound messages routing configuration (which message is redirected to which endpoint).
    /// </summary>
    public interface IOutboundRoutingConfiguration
    {
        /// <summary> Gets the configured outbound routes. </summary>
        IReadOnlyCollection<IOutboundRoute> Routes { get; }

        /// <summary>
        ///     Gets or sets a value indicating whether the messages to be routed through an outbound connector have
        ///     also to be published to the internal bus, to be locally subscribed. The default is <c> false </c>.
        /// </summary>
        bool PublishOutboundMessagesToInternalBus { get; set; }

        /// <summary> Add an outbound routing rule. </summary>
        /// <typeparam name="TMessage"> The type of the messages to be routed. </typeparam>
        /// <param name="router">
        ///     The router to be used to determine the destination endpoint.
        /// </param>
        /// <param name="outboundConnectorType">
        ///     The type of the <see cref="IOutboundConnector" /> to be used. If <c> null </c>, the default
        ///     <see cref="IOutboundConnector" /> will be used.
        /// </param>
        /// <returns>
        ///     The <see cref="IOutboundRoutingConfiguration" /> so that additional calls can be chained.
        /// </returns>
        IOutboundRoutingConfiguration Add<TMessage>(IOutboundRouter router, Type? outboundConnectorType = null);

        /// <summary> Add an outbound routing rule. </summary>
        /// <param name="messageType"> The type of the messages to be routed. </param>
        /// <param name="router">
        ///     The router to be used to determine the destination endpoint.
        /// </param>
        /// <param name="outboundConnectorType">
        ///     The type of the <see cref="IOutboundConnector" /> to be used. If <c> null </c>, the default
        ///     <see cref="IOutboundConnector" /> will be used.
        /// </param>
        /// <returns>
        ///     The <see cref="IOutboundRoutingConfiguration" /> so that additional calls can be chained.
        /// </returns>
        IOutboundRoutingConfiguration Add(Type messageType, IOutboundRouter router, Type? outboundConnectorType = null);

        /// <summary>
        ///     Returns the outbound routes that apply to the specified message.
        /// </summary>
        /// <param name="message"> The message to be routed. </param>
        /// <returns>
        ///     The outbound routes for the specified message.
        /// </returns>
        IReadOnlyCollection<IOutboundRoute> GetRoutesForMessage(object message);
    }
}
