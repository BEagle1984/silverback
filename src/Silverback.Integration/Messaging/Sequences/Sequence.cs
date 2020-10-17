// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Sequences
{
    /// <inheritdoc cref="SequenceBase{TEnvelope}" />
    public abstract class Sequence : SequenceBase<IInboundEnvelope>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Sequence" /> class.
        /// </summary>
        /// <param name="sequenceId">
        ///     The identifier that is used to match the consumed messages with their belonging sequence.
        /// </param>
        /// <param name="context">
        ///     The current <see cref="ConsumerPipelineContext" />, assuming that it will be the one from which the
        ///     sequence gets published to the internal bus.
        /// </param>
        /// <param name="enforceTimeout">
        ///     A value indicating whether the timeout has to be enforced.
        /// </param>
        /// <param name="timeout">
        ///     The timeout to be applied. If not specified the value of <c>Endpoint.Sequence.Timeout</c> will be
        ///     used.
        /// </param>
        protected Sequence(
            string sequenceId,
            ConsumerPipelineContext context,
            bool enforceTimeout = true,
            TimeSpan? timeout = null)
            : base(sequenceId, context, enforceTimeout, timeout)
        {
        }
    }
}
