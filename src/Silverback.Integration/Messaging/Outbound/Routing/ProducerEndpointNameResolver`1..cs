// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Outbound.Routing
{
    /// <summary>
    ///     The base class for a type used to resolve the actual target endpoint name for the outbound message.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages being produced.
    /// </typeparam>
    public abstract class ProducerEndpointNameResolver<TMessage> : IProducerEndpointNameResolver
        where TMessage : class
    {
        /// <inheritdoc cref="IProducerEndpointNameResolver.GetName" />
        public string GetName(IOutboundEnvelope envelope) =>
            GetName((IOutboundEnvelope<TMessage>)envelope);

        /// <summary>
        ///     Gets the actual target endpoint name for the message being produced.
        /// </summary>
        /// <param name="envelope">
        ///     The envelope containing the message being produced.
        /// </param>
        /// <returns>
        ///     The actual name of the endpoint to be produced to.
        /// </returns>
        protected abstract string GetName(IOutboundEnvelope<TMessage> envelope);
    }
}
