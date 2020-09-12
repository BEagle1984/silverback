// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Outbound.Routing
{
    /// <inheritdoc cref="IOutboundRouter{TMessage}" />
    public abstract class OutboundRouter<TMessage> : IOutboundRouter<TMessage>
    {
        /// <inheritdoc cref="IOutboundRouter.Endpoints" />
        public abstract IEnumerable<IProducerEndpoint> Endpoints { get; }

        /// <inheritdoc cref="IOutboundRouter{TMessage}.GetDestinationEndpoints(TMessage,MessageHeaderCollection)" />
        public abstract IEnumerable<IProducerEndpoint> GetDestinationEndpoints(
            TMessage message,
            MessageHeaderCollection headers);

        /// <inheritdoc cref="IOutboundRouter.GetDestinationEndpoints" />
        IEnumerable<IProducerEndpoint> IOutboundRouter.GetDestinationEndpoints(
            object message,
            MessageHeaderCollection headers) =>
            GetDestinationEndpoints((TMessage)message, headers);
    }
}
