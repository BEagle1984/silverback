// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    ///     Routes all messages to a static collection of pre-defined endpoints.
    /// </summary>
    /// <inheritdoc />
    public class StaticOutboundRouter : OutboundRouter<object>
    {
        private readonly IReadOnlyCollection<IProducerEndpoint> _endpoints;

        public StaticOutboundRouter(IEnumerable<IProducerEndpoint> endpoints)
        {
            _endpoints = endpoints.ToList();
        }

        public StaticOutboundRouter(params IProducerEndpoint[] endpoints)
            : this(endpoints.AsEnumerable())
        {
        }

        public override IEnumerable<IProducerEndpoint> Endpoints => _endpoints;

        /// <summary>
        ///     Always returns the endpoints provided in the constructor.
        /// </summary>
        /// <param name="message">The message to be routed.</param>
        /// <param name="headers">The message headers collection.</param>
        public override IEnumerable<IProducerEndpoint> GetDestinationEndpoints(
            object message,
            MessageHeaderCollection headers) =>
            _endpoints;
    }
}