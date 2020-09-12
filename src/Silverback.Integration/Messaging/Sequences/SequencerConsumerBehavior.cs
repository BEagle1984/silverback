// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Sequences
{
    /// <summary>
    ///     Uses the available implementations of <see cref="ISequenceReader"/> to assign the incoming message to the right sequence.
    /// </summary>
    /// <remarks>
    ///    A sequence is a set of messages that are handled as a single unit of work. A sequence could be used to group all chunks belonging to the same source message, all messages belonging to the same data set or to implement batch processing.
    /// </remarks>
    public class SequencerConsumerBehavior : IConsumerBehavior
    {
        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Sequencer;

        /// <inheritdoc cref="IConsumerBehavior.Handle" />
        public async Task Handle(
            ConsumerPipelineContext context,
            ConsumerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            // TODO:
            // * Call each ISequencerReader
            // * Skip until we are at the beginning of a sequence

            await next(context).ConfigureAwait(false);
        }
    }
}
