// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Sequences.Batch
{
    /// <summary>
    ///     Represent an arbitrary sequence of messages created to consume unrelated messages in batch (see
    ///     <see cref="BatchSettings" />).
    /// </summary>
    public class BatchSequence : Sequence<IInboundEnvelope>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BatchSequence" /> class.
        /// </summary>
        /// <param name="sequenceId">
        ///     The identifier that is used to match the consumed messages with their belonging sequence.
        /// </param>
        /// <param name="context">
        ///     The current <see cref="ConsumerPipelineContext" />, assuming that it will be the one from which the
        ///     sequence gets published to the internal bus.
        /// </param>
        public BatchSequence(string sequenceId, ConsumerPipelineContext context)
            : base(
                sequenceId,
                context,
                enforceTimeout: Check.NotNull(context, nameof(context)).Envelope.Endpoint.Batch?.MaxWaitTime != null,
                timeout: Check.NotNull(context, nameof(context)).Envelope.Endpoint.Batch?.MaxWaitTime)
        {
            if (context.Envelope.Endpoint.Batch == null)
                throw new InvalidOperationException("Endpoint.Batch is null.");

            TotalLength = context.Envelope.Endpoint.Batch.Size;
        }

        /// <summary>
        ///     Called when the timout is elapsed. In this special case the sequence is completed instead of aborted.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        protected override Task OnTimeoutElapsedAsync() => CompleteAsync();
    }
}
