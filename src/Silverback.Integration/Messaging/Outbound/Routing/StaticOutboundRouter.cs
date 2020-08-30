// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Outbound.Routing
{
    /// <summary>
    ///     Routes all messages to a static collection of pre-defined endpoints.
    /// </summary>
    public class StaticOutboundRouter : OutboundRouter<object>
    {
        private readonly IReadOnlyCollection<IProducerEndpoint> _endpoints;

        /// <summary>
        ///     Initializes a new instance of the <see cref="StaticOutboundRouter" /> class.
        /// </summary>
        /// <param name="endpoints">
        ///     The endpoints to route the messages to.
        /// </param>
        public StaticOutboundRouter(IEnumerable<IProducerEndpoint> endpoints)
        {
            _endpoints = endpoints.ToList();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="StaticOutboundRouter" /> class.
        /// </summary>
        /// <param name="endpoints">
        ///     The endpoints to route the messages to.
        /// </param>
        public StaticOutboundRouter(params IProducerEndpoint[] endpoints)
            : this(endpoints.AsEnumerable())
        {
        }

        /// <inheritdoc cref="OutboundRouter{TMessage}.Endpoints" />
        public override IEnumerable<IProducerEndpoint> Endpoints => _endpoints;

        /// <inheritdoc cref="OutboundRouter{TMessage}.GetDestinationEndpoints" />
        /// <remarks>
        ///     Always returns the endpoints provided in the constructor.
        /// </remarks>
        public override IEnumerable<IProducerEndpoint> GetDestinationEndpoints(
            object message,
            MessageHeaderCollection headers) =>
            _endpoints;
    }
}
