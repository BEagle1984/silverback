// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    ///     Routes the outbound messages to one outbound endpoint.
    /// </summary>
    /// <inheritdoc />
    public abstract class SimpleOutboundRouter<TMessage> : OutboundRouter<TMessage>
    {
        public override IEnumerable<IProducerEndpoint> GetDestinationEndpoints(
            TMessage message,
            MessageHeaderCollection headers) =>
            new[] { GetDestinationEndpoint(message, headers) };

        /// <summary>
        ///     Returns the <see cref="IProducerEndpoint" /> where the specified message must be published.
        ///     When <c>null</c> is returned, the message will not be be published.
        /// </summary>
        /// <param name="message">The message to be routed.</param>
        /// <param name="headers">The message headers collection.</param>
        protected abstract IProducerEndpoint GetDestinationEndpoint(TMessage message, MessageHeaderCollection headers);
    }
}