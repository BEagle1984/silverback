// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    /// Holds the outbound messages routing configuration (which message is redirected to which endpoint).
    /// </summary>
    public interface IOutboundRoutingConfiguration
    {
        /// <summary>
        /// A boolean value indicating whether the messages to be routed
        /// through an outbound connector have also to be published to the
        /// internal bus, to be locally subscribed. The default is <c>false</c>.
        /// </summary>
        bool PublishOutboundMessagesToInternalBus { get; set; }

        /// <summary>
        /// The configured outbound routes.
        /// </summary>
        IEnumerable<IOutboundRoute> Routes { get; }

        /// <summary>
        /// Add an outbound routing rule.
        /// </summary>
        /// <typeparam name="TMessage">The type of the messages to be routed.</typeparam>
        /// <param name="endpoint">The destination endpoint.</param>
        /// <param name="outboundConnectorType">The type of the <see cref="IOutboundConnector"/> to be used</param>
        /// <returns></returns>
        IOutboundRoutingConfiguration Add<TMessage>(IProducerEndpoint endpoint, Type outboundConnectorType = null);

        /// <summary>
        /// Add an outbound routing rule.
        /// </summary>
        /// <param name="messageType">The type of the messages to be routed.</param>
        /// <param name="endpoint">The destination endpoint.</param>
        /// <param name="outboundConnectorType">The type of the <see cref="IOutboundConnector"/> to be used</param>
        /// <returns></returns>
        IOutboundRoutingConfiguration Add(Type messageType, IProducerEndpoint endpoint, Type outboundConnectorType = null);

        /// <summary>
        /// Returns the outbound routes that apply to the specified message.
        /// </summary>
        /// <param name="message">The message to be routed.</param>
        /// <returns></returns>
        IEnumerable<IOutboundRoute> GetRoutesForMessage(object message);
    }
}