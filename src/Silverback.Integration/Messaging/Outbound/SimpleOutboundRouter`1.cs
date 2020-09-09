// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    ///     Routes the outbound messages to one outbound endpoint.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be routed.
    /// </typeparam>
    public abstract class SimpleOutboundRouter<TMessage> : OutboundRouter<TMessage>
    {
        /// <inheritdoc cref="OutboundRouter{TMessage}.GetDestinationEndpoints" />
        public override IEnumerable<IProducerEndpoint> GetDestinationEndpoints(
            TMessage message,
            MessageHeaderCollection headers)
        {
            var endpoint = GetDestinationEndpoint(message, headers);

            return endpoint != null
                ? new[] { endpoint }
                : Array.Empty<IProducerEndpoint>();
        }

        /// <summary>
        ///     Returns the <see cref="IProducerEndpoint" /> representing the endpoint where the specified message
        ///     must be produced. When <c>null</c> is returned, the message will not be be published.
        /// </summary>
        /// <param name="message">
        ///     The message to be routed.
        /// </param>
        /// <param name="headers">
        ///     The message headers collection.
        /// </param>
        /// <returns>
        ///     The endpoint to route the message to, or <c>null</c> if the message doesn't have to be routed.
        /// </returns>
        protected abstract IProducerEndpoint? GetDestinationEndpoint(TMessage message, MessageHeaderCollection headers);
    }
}
