// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Outbound.Routing
{
    /// <summary>
    ///     Holds the outbound messages routing configuration (which message is redirected to which endpoint).
    /// </summary>
    public interface IOutboundRoutingConfiguration
    {
        /// <summary>
        ///     Gets the configured outbound routes.
        /// </summary>
        IReadOnlyCollection<IOutboundRoute> Routes { get; }

        /// <summary>
        ///     Gets or sets a value indicating whether the messages to be routed through an outbound connector have
        ///     also to be published to the internal bus, to be locally subscribed. The default is <c>false</c>.
        /// </summary>
        bool PublishOutboundMessagesToInternalBus { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether the registration of endpoints is idempotent. This means that
        ///     an endpoint for the same message type and the same name cannot be registered multiple times. The default
        ///     is <c>true</c>.
        /// </summary>
        bool IdempotentEndpointRegistration { get; set; }

        /// <summary>
        ///     Add an outbound routing rule.
        /// </summary>
        /// <typeparam name="TMessage">
        ///     The type of the messages to be routed.
        /// </typeparam>
        /// <param name="outboundRouterFactory">
        ///     The factory method to be used to get the instance of <see cref="IOutboundRouter" /> to be used to
        ///     determine the destination endpoint.
        /// </param>
        /// <returns>
        ///     The <see cref="IOutboundRoutingConfiguration" /> so that additional calls can be chained.
        /// </returns>
        IOutboundRoutingConfiguration Add<TMessage>(Func<IServiceProvider, IOutboundRouter> outboundRouterFactory);

        /// <summary>
        ///     Add an outbound routing rule.
        /// </summary>
        /// <param name="messageType">
        ///     The type of the messages to be routed.
        /// </param>
        /// <param name="outboundRouterFactory">
        ///     The factory method to be used to get the instance of <see cref="IOutboundRouter" /> to be used to
        ///     determine the destination endpoint.
        /// </param>
        /// <returns>
        ///     The <see cref="IOutboundRoutingConfiguration" /> so that additional calls can be chained.
        /// </returns>
        IOutboundRoutingConfiguration Add(
            Type messageType,
            Func<IServiceProvider, IOutboundRouter> outboundRouterFactory);

        /// <summary>
        ///     Returns the outbound routes that apply to the specified message.
        /// </summary>
        /// <param name="message">
        ///     The message to be routed.
        /// </param>
        /// <returns>
        ///     The outbound routes for the specified message.
        /// </returns>
        IReadOnlyCollection<IOutboundRoute> GetRoutesForMessage(object message);

        /// <summary>
        ///     Returns the outbound routes that apply to a message of the specified message.
        /// </summary>
        /// <param name="messageType">
        ///     The type of the message to be routed.
        /// </param>
        /// <returns>
        ///     The outbound routes for the specified message.
        /// </returns>
        IReadOnlyCollection<IOutboundRoute> GetRoutesForMessage(Type messageType);
    }
}
