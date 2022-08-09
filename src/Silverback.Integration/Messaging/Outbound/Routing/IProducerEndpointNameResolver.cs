// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Outbound.Routing
{
    /// <summary>
    ///     Represents a type used to resolve the actual target endpoint name for the outbound message.
    /// </summary>
    public interface IProducerEndpointNameResolver
    {
        /// <summary>
        ///     Gets the actual target endpoint name for the message being produced. If it returns
        ///     <c>null</c> the message will not be produced.
        /// </summary>
        /// <param name="envelope">
        ///     The envelope containing the message being produced.
        /// </param>
        /// <returns>
        ///     The actual name of the endpoint to be produced to.
        /// </returns>
        string? GetName(IOutboundEnvelope envelope);
    }
}
