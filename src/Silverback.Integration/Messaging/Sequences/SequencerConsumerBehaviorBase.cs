// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Sequences
{
    /// <summary>
    ///     Uses the available implementations of <see cref="ISequenceReader" /> to assign the incoming message to
    ///     the right sequence.
    /// </summary>
    /// <remarks>
    ///     A sequence is a set of messages that are handled as a single unit of work. A sequence could be used to
    ///     group all chunks belonging to the same source message, all messages belonging to the same data set or
    ///     to implement batch processing.
    /// </remarks>
    public abstract class SequencerConsumerBehaviorBase : IConsumerBehavior
    {
        private readonly IReadOnlyCollection<ISequenceReader> _sequenceReaders;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SequencerConsumerBehaviorBase" /> class.
        /// </summary>
        /// <param name="sequenceReaders">
        ///     The <see cref="ISequenceReader" /> implementations to be used.
        /// </param>
        protected SequencerConsumerBehaviorBase(IEnumerable<ISequenceReader> sequenceReaders)
        {
            _sequenceReaders = sequenceReaders.SortBySortIndex().ToList();
        }

        /// <inheritdoc cref="ISorted.SortIndex" />
        public abstract int SortIndex { get; }

        /// <inheritdoc cref="IConsumerBehavior.HandleAsync" />
        public async Task HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            var sequenceReader = await GetSequenceReaderAsync(context).ConfigureAwait(false);

            if (sequenceReader == null)
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            // Store the original envelope in case it gets replaced in the GetSequence method (see ChunkSequenceReader)
            var originalEnvelope = context.Envelope;
            var sequence = await sequenceReader.GetSequenceAsync(context).ConfigureAwait(false);

            if (sequence != null)
            {
                ((RawInboundEnvelope)context.Envelope).Sequence = sequence;

                if (sequence.IsNew)
                {
                    await PublishSequenceAsync(context, next).ConfigureAwait(false);

                    if (context.ProcessingTask != null)
                        MonitorProcessingTaskPrematureCompletion(context.ProcessingTask, sequence);
                }

                await sequence.AddAsync(originalEnvelope).ConfigureAwait(false);
            }
        }

        /// <summary>
        ///     Forwards the new sequence to the next behavior in the pipeline.
        /// </summary>
        /// <param name="context">
        ///     The context that is passed along the behaviors pipeline.
        /// </param>
        /// <param name="next">
        ///     The next behavior in the pipeline.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        protected abstract Task PublishSequenceAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next);

        private static void MonitorProcessingTaskPrematureCompletion(Task processingTask, ISequence sequence)
        {
            Task.Run(
                async () =>
                {
                    try
                    {
                        await processingTask.ConfigureAwait(false);

                        if (!sequence.IsPending)
                            return;

                        // Abort the uncompleted sequence if the processing task completes, to avoid unreleased locks.
                        await sequence.AbortAsync(SequenceAbortReason.EnumerationAborted).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        // TODO: Log?

                        if (!sequence.IsPending)
                            return;

                        await sequence.AbortAsync(SequenceAbortReason.Error, exception)
                            .ConfigureAwait(false);
                    }
                });
        }

        // TODO: Implement FirstOrDefaultAsync
        private async Task<ISequenceReader?> GetSequenceReaderAsync(ConsumerPipelineContext context)
        {
            foreach (var reader in _sequenceReaders)
            {
                if (await reader.CanHandleAsync(context).ConfigureAwait(false))
                    return reader;
            }

            return null;
        }
    }
}
