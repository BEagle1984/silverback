// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    ///     Routes the outbound messages to one or multiple outbound endpoints.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be routed.
    /// </typeparam>
    public interface IOutboundRouter<TMessage> : IOutboundRouter
    {
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
        ///     The endpoints to route the message to.
        /// </returns>
        IEnumerable<IProducerEndpoint> GetDestinationEndpoints(
            TMessage message,
            MessageHeaderCollection headers);
    }
}
