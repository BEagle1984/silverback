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
        public SequencerConsumerBehaviorBase(IEnumerable<ISequenceReader> sequenceReaders)
        {
            _sequenceReaders = sequenceReaders.SortBySortIndex().ToList();
        }

        /// <inheritdoc cref="ISorted.SortIndex" />
        public abstract int SortIndex { get; }

        /// <inheritdoc cref="IConsumerBehavior.Handle" />
        public async Task Handle(ConsumerPipelineContext context, ConsumerBehaviorHandler next)
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
                    // Start a separate Task to process the sequence. It will be awaited when the last message is
                    // consumed.
                    context.ProcessingTask = Task.Run(async () => await next(context).ConfigureAwait(false));

                    CheckPrematureCompletion(context);
                }

                await sequence.AddAsync(originalEnvelope).ConfigureAwait(false);
            }
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

        private static void CheckPrematureCompletion(ConsumerPipelineContext context)
        {
            if (context.ProcessingTask == null || context.Sequence == null)
                return;

            Task.Run(
                async () =>
                {
                    try
                    {
                        await context.ProcessingTask.ConfigureAwait(false);

                        // Abort the uncompleted sequence if the processing task completes, to avoid unreleased locks.
                        if (!context.Sequence.IsComplete)
                            await context.Sequence.AbortAsync(SequenceAbortReason.EnumerationAborted).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        // TODO: Log?

                        if (context.Sequence.IsPending)
                            await context.Sequence.AbortAsync(SequenceAbortReason.Error).ConfigureAwait(false);
                    }
                });
        }
    }
}
