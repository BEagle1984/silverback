// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Sequences;

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

    private readonly ISilverbackLogger<SequencerConsumerBehaviorBase> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SequencerConsumerBehaviorBase" /> class.
    /// </summary>
    /// <param name="sequenceReaders">
    ///     The <see cref="ISequenceReader" /> implementations to be used.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="ISilverbackLogger" />.
    /// </param>
    protected SequencerConsumerBehaviorBase(
        IEnumerable<ISequenceReader> sequenceReaders,
        ISilverbackLogger<SequencerConsumerBehaviorBase> logger)
    {
        _sequenceReaders = Check.NotNull(sequenceReaders, nameof(sequenceReaders)).SortBySortIndex();
        _logger = Check.NotNull(logger, nameof(logger));
    }

    /// <inheritdoc cref="ISorted.SortIndex" />
    public abstract int SortIndex { get; }

    /// <inheritdoc cref="IConsumerBehavior.HandleAsync" />
    public virtual async ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next, CancellationToken cancellationToken)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        ISequenceReader? sequenceReader = await _sequenceReaders
            .FirstOrDefaultAsync(reader => reader.CanHandleAsync(context))
            .ConfigureAwait(false);

        if (sequenceReader == null)
        {
            await next(context, cancellationToken).ConfigureAwait(false);
            return;
        }

        // Store the original envelope in case it is replaced in the GetSequence method (see ChunkSequenceReader)
        IRawInboundEnvelope originalEnvelope = context.Envelope;

        // Store the previous sequence since it must be added to the new one (e.g. ChunkSequence into BatchSequence)
        ISequence? previousSequence = context.Sequence;

        ISequence? sequence = await AddMessageToSequenceAsync(
                context,
                next,
                sequenceReader,
                originalEnvelope,
                previousSequence,
                cancellationToken)
            .ConfigureAwait(false);

        await HandleCompletedSequenceAsync(context, sequence).ConfigureAwait(false);
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
    /// <param name="cancellationToken">
    ///     The cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    protected abstract ValueTask PublishSequenceAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next, CancellationToken cancellationToken);

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
    protected virtual ValueTask AwaitOtherBehaviorIfNeededAsync(ISequence sequence) => default;

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception passed to AbortAsync to be logged and forwarded.")]
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

                        await sequence.AbortAsync(SequenceAbortReason.Error, exception).ConfigureAwait(false);
                    }
                })
            .FireAndForget();
    }

    private async Task<ISequence?> AddMessageToSequenceAsync(
        ConsumerPipelineContext context,
        ConsumerBehaviorHandler next,
        ISequenceReader sequenceReader,
        IRawInboundEnvelope originalEnvelope,
        ISequence? previousSequence,
        CancellationToken cancellationToken)
    {
        ISequence? sequence;
        AddToSequenceResult addToSequenceResult;

        // Loop to handle edge cases where the sequence gets completed (e.g. because of timeout) between the calls to
        // GetSequenceAsync and AddAsync
        do
        {
            sequence = await GetSequenceAsync(context, sequenceReader).ConfigureAwait(false);

            if (sequence == null)
                return null;

            Activity.Current?.SetTag(ActivityTagNames.SequenceId, sequence.SequenceId);

            await PublishSequenceIfNewAsync(context, next, sequence, cancellationToken).ConfigureAwait(false);

            addToSequenceResult = await sequence.AddAsync(
                    originalEnvelope,
                    previousSequence,
                    context.Envelope.Endpoint.Configuration.ThrowIfUnhandled)
                .ConfigureAwait(false);

            // If the sequence was new, it means it was never handed over to the transaction handler
            // (no message was added to the sequence so far, the timeout elapsed before the sequence
            // was used).
            if (!addToSequenceResult.IsSuccess && sequence.IsNew)
            {
                await context.SequenceStore.RemoveAsync(sequence.SequenceId).ConfigureAwait(false);
                sequence.Dispose();
            }
        }
        while (addToSequenceResult is { IsSuccess: false, IsAborted: false });

        if (addToSequenceResult.IsAborted)
        {
            // Ensure that the sequence is really aborted before returning,otherwise the TransactionHandlerConsumerBehavior could
            // continue before the abort and miss the exception
            if (addToSequenceResult.AbortTask != null)
                await addToSequenceResult.AbortTask.ConfigureAwait(false);
        }
        else
        {
            _logger.LogMessageAddedToSequence(context.Envelope, sequence);
        }

        return sequence;
    }

    private async Task HandleCompletedSequenceAsync(ConsumerPipelineContext context, ISequence? sequence)
    {
        if (sequence is not { IsComplete: true })
            return;

        await AwaitOtherBehaviorIfNeededAsync(sequence).ConfigureAwait(false);

        // Mark the envelope as the end of the sequence only if the sequence wasn't swapped (e.g. chunk -> batch)
        if (sequence.Context.Sequence == null || sequence == sequence.Context.Sequence ||
            sequence.Context.Sequence.IsCompleting || sequence.Context.Sequence.IsComplete)
        {
            context.SetIsSequenceEnd();
        }

        _logger.LogSequenceCompleted(sequence);
    }

    private async Task<ISequence?> GetSequenceAsync(ConsumerPipelineContext context, ISequenceReader sequenceReader)
    {
        ISequence sequence = await sequenceReader.GetSequenceAsync(context).ConfigureAwait(false);

        if (sequence.IsComplete)
            return null;

        if (sequence is IncompleteSequence incompleteSequence)
        {
            _logger.LogIncompleteSequenceSkipped(incompleteSequence);
            return null;
        }

        context.SetSequence(sequence, sequence.IsNew);

        return sequence;
    }

    private async Task PublishSequenceIfNewAsync(
        ConsumerPipelineContext context,
        ConsumerBehaviorHandler next,
        ISequence sequence,
        CancellationToken cancellationToken)
    {
        if (sequence.IsNew)
        {
            await PublishSequenceAsync(context, next, cancellationToken).ConfigureAwait(false);

            if (context.ProcessingTask != null)
                MonitorProcessingTaskPrematureCompletion(context.ProcessingTask, sequence);

            _logger.LogSequenceStarted(sequence);
        }
    }
}
