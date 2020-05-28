// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors
{
    /// <inheritdoc />
    public abstract class OutboundRouter<TMessage> : IOutboundRouter<TMessage>
    {
        /// <inheritdoc />
        public abstract IEnumerable<IProducerEndpoint> Endpoints { get; }

        /// <inheritdoc />
        public abstract IEnumerable<IProducerEndpoint> GetDestinationEndpoints(
            TMessage message,
            MessageHeaderCollection headers);

        /// <inheritdoc />
        IEnumerable<IProducerEndpoint> IOutboundRouter.GetDestinationEndpoints(
            object message,
            MessageHeaderCollection headers) =>
            GetDestinationEndpoints((TMessage)message, headers);
    }
}
