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

            // TODO: Task.Run is needed???

            // Start a separate Task to process the sequence. It will be awaited when the last message is
            // consumed.
            // if (context.ProcessingTask != null) // TODO: Needed to combine chunk and batch?
            // {
            //     var previousProcessingTask = context.ProcessingTask;
            //
            //     context.ProcessingTask = Task.Run(
            //         async () =>
            //         {
            //             await Task.WhenAll(next(context), previousProcessingTask).ConfigureAwait(false);
            //         });
            // }
            // else
            // {
            context.ProcessingTask = Task.Run(async () => await next(context).ConfigureAwait(false));
            // }

            return Task.CompletedTask;
        }
    }
}
