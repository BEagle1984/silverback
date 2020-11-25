﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
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

        private readonly ISilverbackIntegrationLogger<SequencerConsumerBehaviorBase> _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SequencerConsumerBehaviorBase" /> class.
        /// </summary>
        /// <param name="sequenceReaders">
        ///     The <see cref="ISequenceReader" /> implementations to be used.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackIntegrationLogger" />.
        /// </param>
        protected SequencerConsumerBehaviorBase(
            IEnumerable<ISequenceReader> sequenceReaders,
            ISilverbackIntegrationLogger<SequencerConsumerBehaviorBase> logger)
        {
            _sequenceReaders = Check.NotNull(sequenceReaders, nameof(sequenceReaders)).SortBySortIndex().ToList();
            _logger = Check.NotNull(logger, nameof(logger));
        }

        /// <inheritdoc cref="ISorted.SortIndex" />
        public abstract int SortIndex { get; }

        /// <inheritdoc cref="IConsumerBehavior.HandleAsync" />
        public virtual async Task HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            var sequenceReader = await GetSequenceReaderAsync(context).ConfigureAwait(false);

            if (sequenceReader == null)
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            // Store the original envelope in case it is replaced in the GetSequence method (see ChunkSequenceReader)
            var originalEnvelope = context.Envelope;

            // Store the previous sequence since it must be added to the new one (e.g. ChunkSequence into BatchSequence)
            var previousSequence = context.Sequence;

            var sequence = await sequenceReader.GetSequenceAsync(context).ConfigureAwait(false);

            if (sequence == null)
            {
                _logger.LogWarningWithMessageInfo(
                    IntegrationEventIds.IncompleteSequenceDiscarded,
                    "The incomplete sequence is ignored (probably missing the first message).",
                    context);

                return;
            }

            context.SetSequence(sequence, sequence.IsNew);

            if (sequence.IsNew)
            {
                await PublishSequenceAsync(context, next).ConfigureAwait(false);

                if (context.ProcessingTask != null)
                    MonitorProcessingTaskPrematureCompletion(context.ProcessingTask, sequence);

                _logger.LogDebugWithMessageInfo(
                    IntegrationEventIds.SequenceStarted,
                    $"Started new {sequence.GetType().Name}.",
                    context);
            }

            await sequence.AddAsync(originalEnvelope, previousSequence).ConfigureAwait(false);

            _logger.LogDebugWithMessageInfo(
                IntegrationEventIds.MessageAddedToSequence,
                $"Message added to {sequence.GetType().Name}.",
                context);

            if (sequence.IsComplete)
            {
                await AwaitOtherBehaviorIfNeededAsync(sequence).ConfigureAwait(false);

                // Mark the envelope as the end of the sequence only if the sequence wasn't swapped (e.g. chunk -> batch)
                if (sequence.Context.Sequence == null || sequence == sequence.Context.Sequence ||
                    sequence.Context.Sequence.IsCompleting || sequence.Context.Sequence.IsComplete)
                {
                    context.SetIsSequenceEnd();
                }

                _logger.LogDebugWithMessageInfo(
                    IntegrationEventIds.SequenceCompleted,
                    $"{sequence.GetType().Name} '{sequence.SequenceId}' completed.",
                    context);
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

        /// <summary>
        ///     When overridden in a derived class awaits for the sequence to be processed by the other twin behavior.
        ///     This is used to have the <see cref="RawSequencerConsumerBehavior" /> wait for the processing by the
        ///     <see cref="SequencerConsumerBehavior" /> and it's needed to be able to properly determine the sequence
        ///     end in the case where a ChunkSequence is added into another sequence (e.g. BatchSequence).
        /// </summary>
        /// <param name="sequence">
        ///     The current sequence.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        protected virtual Task AwaitOtherBehaviorIfNeededAsync(ISequence sequence) => Task.CompletedTask;

        [SuppressMessage("", "CA1031", Justification = "Exception passed to AbortAsync to be logged and forwarded.")]
        private static void MonitorProcessingTaskPrematureCompletion(Task processingTask, ISequence sequence)
        {
            if (sequence.ParentSequence != null)
                return;

            Task.Run(
                async () =>
                {
                    try
                    {
                        await processingTask.ConfigureAwait(false);

                        // Abort only parent sequences and don't consider the enumeration as aborted if the
                        // sequence is actually complete
                        if (sequence.ParentSequence != null || sequence.IsComplete)
                            return;

                        // Call AbortAsync to abort the uncompleted sequence, to avoid unreleased locks.
                        // The reason behind this call here may be counterintuitive but with
                        // SequenceAbortReason.EnumerationAborted a commit is in fact performed.
                        await sequence.AbortAsync(SequenceAbortReason.EnumerationAborted).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        if (!sequence.IsPending || sequence.ParentSequence != null)
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
