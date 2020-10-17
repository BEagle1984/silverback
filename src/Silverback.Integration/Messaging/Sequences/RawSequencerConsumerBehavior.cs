// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Sequences
{
    /// <inheritdoc cref="SequencerConsumerBehaviorBase" />
    public class RawSequencerConsumerBehavior : SequencerConsumerBehaviorBase
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RawSequencerConsumerBehavior" /> class.
        /// </summary>
        /// <param name="sequenceReaders">
        ///     The <see cref="ISequenceReader" /> implementations to be used.
        /// </param>
        public RawSequencerConsumerBehavior(IEnumerable<ISequenceReader> sequenceReaders)
            : base(sequenceReaders.Where(reader => reader.HandlesRawMessages))
        {
        }

        /// <inheritdoc cref="ISorted.SortIndex" />
        public override int SortIndex => BrokerBehaviorsSortIndexes.Consumer.RawSequencer;

        /// <inheritdoc cref="SequencerConsumerBehaviorBase.PublishSequenceAsync" />
        protected override Task PublishSequenceAsync(
            ConsumerPipelineContext context,
            ConsumerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            context.ProcessingTask = Task.Run(async () => await next(context).ConfigureAwait(false));

            return Task.CompletedTask;
        }

        /// <inheritdoc cref="SequencerConsumerBehaviorBase.AwaitOtherBehaviorIfNeededAsync" />
        protected override async Task AwaitOtherBehaviorIfNeededAsync(ISequence sequence)
        {
            if (sequence is ISequenceImplementation sequenceImpl)
                await sequenceImpl.SequencerBehaviorsTaskCompletionSource.Task.ConfigureAwait(false);
        }
    }
}
