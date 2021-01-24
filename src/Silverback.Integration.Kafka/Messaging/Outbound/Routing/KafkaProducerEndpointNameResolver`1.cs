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
    public abstract class KafkaProducerEndpointNameResolver<TMessage>
        : ProducerEndpointNameResolver<TMessage>, IKafkaProducerEndpointNameResolver
        where TMessage : class
    {
        /// <inheritdoc cref="IKafkaProducerEndpointNameResolver.GetPartition" />
        public int? GetPartition(IOutboundEnvelope envelope) =>
            GetPartition((IOutboundEnvelope<TMessage>)envelope);

        /// <summary>
        ///     Gets the target partition for the message being produced.
        /// </summary>
        /// <param name="envelope">
        ///     The envelope containing the message being produced.
        /// </param>
        /// <returns>
        ///     The target partition index. If <c>null</c> the partition is automatically derived from the message key
        ///     (use <see cref="KafkaKeyMemberAttribute" /> to specify a message key, otherwise a random one will be
        ///     generated).
        /// </returns>
        protected abstract int? GetPartition(IOutboundEnvelope<TMessage> envelope);
    }
}
