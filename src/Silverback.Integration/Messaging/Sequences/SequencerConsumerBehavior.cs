// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Sequences
{
    /// <inheritdoc cref="SequencerConsumerBehaviorBase" />
    public class SequencerConsumerBehavior : SequencerConsumerBehaviorBase
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SequencerConsumerBehavior" /> class.
        /// </summary>
        /// <param name="sequenceReaders">
        ///     The <see cref="ISequenceReader" /> implementations to be used.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackLogger" />.
        /// </param>
        public SequencerConsumerBehavior(
            IEnumerable<ISequenceReader> sequenceReaders,
            ISilverbackLogger<SequencerConsumerBehavior> logger)
            : base(sequenceReaders.Where(reader => !reader.HandlesRawMessages), logger)
        {
        }

        /// <inheritdoc cref="ISorted.SortIndex" />
        public override int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Sequencer;

        /// <inheritdoc cref="SequencerConsumerBehaviorBase.HandleAsync" />
        public override async Task HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next)
        {
            var rawSequence = Check.NotNull(context, nameof(context)).Sequence as ISequenceImplementation;

            try
            {
                await base.HandleAsync(context, next).ConfigureAwait(false);

                // Abort all pending sequences if the current message doesn't belong to a sequence
                if (context.Sequence == null)
                {
                    await context.SequenceStore
                        .GetPendingSequences()
                        .ForEachAsync(sequence => sequence.AbortAsync(SequenceAbortReason.IncompleteSequence))
                        .ConfigureAwait(false);
                }
            }
            finally
            {
                rawSequence?.CompleteSequencerBehaviorsTask();
            }
        }

        /// <inheritdoc cref="SequencerConsumerBehaviorBase.PublishSequenceAsync" />
        protected override Task PublishSequenceAsync(
            ConsumerPipelineContext context,
            ConsumerBehaviorHandler next)
        {
            Check.NotNull(next, nameof(next));

            return next(context);
        }
    }
}
