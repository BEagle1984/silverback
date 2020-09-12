// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    ///     Routes the outbound messages to one or multiple outbound endpoints.
    /// </summary>
    public interface IOutboundRouter
    {
        /// <summary>
        ///     Gets the endpoints that are potentially targeted by this router. This collection could be built over
        ///     time in case of a dynamic <see cref="IOutboundRouter" /> but that will prevent the
        ///     <see cref="OutboundQueueWorker" /> to work properly and it's not optimal as it used for example by the
        ///     health checks to ping all possible endpoints.
        /// </summary>
        IEnumerable<IProducerEndpoint> Endpoints { get; }

        /// <summary>
        ///     Returns the collection of <see cref="IProducerEndpoint" /> representing the endpoints where the
        ///     specified message must be produced.
        /// </summary>
        /// <param name="message">
        ///     The message to be routed.
        /// </param>
        /// <param name="headers">
        ///     The message headers collection.
        /// </param>
        /// <returns>
        ///     The endpoints to produce to.
        /// </returns>
        IEnumerable<IProducerEndpoint> GetDestinationEndpoints(object message, MessageHeaderCollection headers);
    }
}
