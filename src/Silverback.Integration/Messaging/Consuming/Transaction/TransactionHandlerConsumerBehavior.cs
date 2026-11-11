// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Unbounded;
using Silverback.Util;

namespace Silverback.Messaging.Consuming.Transaction;

/// <summary>
///     Handles the consumer transaction and applies the error policies.
/// </summary>
public class TransactionHandlerConsumerBehavior : IConsumerBehavior
{
    private readonly ISilverbackLogger<TransactionHandlerConsumerBehavior> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TransactionHandlerConsumerBehavior" /> class.
    /// </summary>
    /// <param name="logger">
    ///     The <see cref="ISilverbackLogger{TCategoryName}" />.
    /// </param>
    public TransactionHandlerConsumerBehavior(ISilverbackLogger<TransactionHandlerConsumerBehavior> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.TransactionHandler;

    /// <inheritdoc cref="IConsumerBehavior.HandleAsync" />
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Exception rethrown by the Subscribe method")]
    public async ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next, CancellationToken cancellationToken)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        bool isSequenceProcessingAwaiterStarted = false;

        try
        {
            IServiceScope scope = context.ServiceProvider.CreateScope();
            context.ReplaceServiceScope(scope);
            context.TransactionManager = new ConsumerTransactionManager(
                context,
                context.ServiceProvider.GetRequiredService<ISilverbackLogger<ConsumerTransactionManager>>());
            context.SilverbackContext.SetConsumerPipelineContext(context);

            _logger.LogProcessing(context.Envelope);

            await next(context, cancellationToken).ConfigureAwait(false);

            if (context.Sequence == null)
            {
                await context.TransactionManager.CommitAsync().ConfigureAwait(false);
                context.Dispose();
            }
            else
            {
                if (context.IsSequenceStart)
                {
                    StartSequenceProcessingAwaiter(context, cancellationToken);
                    isSequenceProcessingAwaiterStarted = true;
                }

                await AwaitProcessedIfNecessaryAsync(context).ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            // Sequence errors are handled in AwaitSequenceProcessingAsync, just await the rollback and rethrow
            // (-> if the exception bubbled up till this point, it's because it failed to be handled, and it's safer to stop the consumer)
            if (context.Sequence != null)
            {
                bool isSequenceExceptionHandled = await HandleSequenceExceptionAsync(
                        context,
                        context.Sequence,
                        exception,
                        isSequenceProcessingAwaiterStarted,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (!isSequenceExceptionHandled)
                    throw;

                return;
            }

            // The cancellation requested by the consumer stop isn't a processing failure: roll back without applying the error policies
            if (IsStopCancellation(context, exception, cancellationToken))
            {
                await RollbackStopCancellationAsync(context, exception).ConfigureAwait(false);
                return;
            }

            if (!await HandleExceptionAsync(context, exception).ConfigureAwait(false))
                throw;
        }
    }

    // Check IsCompleting first, since it's reset only after the state is switched to complete
    private static bool IsCompletingOrComplete(ISequence sequence) => sequence.IsCompleting || sequence.IsComplete;

    // When streaming, the exception might come from the unbounded sequence processing: rely on its abort reason, like for the other
    // sequences, since the token might have been canceled in the meantime by a stop that didn't cause the exception. Rely on the token
    // for the exceptions thrown by the rest of the pipeline (e.g., by the other subscribers or by the early exit handling, which rolls
    // back the message when the consumer is stopping), as for the messages that aren't streamed.
    private static bool IsStopCancellation(ConsumerPipelineContext context, Exception exception, CancellationToken cancellationToken) =>
        context.UnboundedSequence is { AbortReason: not SequenceAbortReason.None } unboundedSequence &&
        exception == unboundedSequence.AbortException
            ? StopCancellationHelper.IsStopCancellation(exception, unboundedSequence)
            : StopCancellationHelper.IsStopCancellation(exception, cancellationToken);

    // The unbounded sequence abort applied the error policies already, because the subscriber failed while no message was being added:
    // the message added afterwards just rolls back and stops the consumer
    private static bool IsHandledByUnboundedSequenceAbort(ConsumerPipelineContext context, Exception exception) =>
        context.UnboundedSequence is UnboundedSequence { IsErrorHandledByAbort: true } unboundedSequence &&
        exception == unboundedSequence.AbortException;

    // The processing of the outer sequence started by the context (e.g., the batch started by a chunked message) can fail while the
    // inner sequence awaited first, which is complete already, is still referenced. The failed processing task aborts the outer sequence
    // with its exception, applying the error policies, before rethrowing it (see PublisherConsumerBehavior): abort the outer sequence
    // again, just awaiting that abort, since aborting the inner sequence would apply the error policies a second time. Otherwise the
    // exception comes from the inner sequence processing (e.g., failing before the message is added to the outer sequence).
    private static ISequence GetFailedSequence(ConsumerPipelineContext context, ISequence sequence, Exception exception) =>
        context is { IsSequenceStart: true, Sequence: { } outerSequence } && outerSequence != sequence &&
        outerSequence.AbortException == exception
            ? outerSequence
            : sequence;

    private async Task AwaitProcessedIfNecessaryAsync(ConsumerPipelineContext context)
    {
        if (context.Sequence == null)
            throw new InvalidOperationException("Sequence is null");

        // At the end of the sequence (or when the processing task exits prematurely), ensure that the
        // commit was performed or the error policies were applied before continuing
        if (context.IsSequenceEnd || context.Sequence.IsAborted)
        {
            _logger.LogSequenceEndedAwaitingProcessingTask(context.Envelope);

            try
            {
                await context.Sequence.AwaitProcessingAsync(true).ConfigureAwait(false);
            }
            finally
            {
                _logger.LogSequenceEndedProcessingTaskCompleted(context.Envelope);
            }
        }

        // The processing take place in the context of the first element, and we can therefore dispose the further contexts immediately
        if (!context.IsSequenceStart)
            context.Dispose();
    }

    // The token is only used to recognize the cancellation requested by the consumer stop, the awaiter must run until completion
    private void StartSequenceProcessingAwaiter(ConsumerPipelineContext context, CancellationToken cancellationToken) =>
        Task.Run(() => AwaitSequenceProcessingAsync(context, cancellationToken), CancellationToken.None).FireAndForget();

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception passed to AbortAsync")]
    private async Task AwaitSequenceProcessingAsync(ConsumerPipelineContext context, CancellationToken cancellationToken)
    {
        ISequence sequence = context.Sequence ?? throw new InvalidOperationException("Sequence is null.");
        context = sequence.Context;
        ISequence? failedSequence = null;

        try
        {
            (context, sequence) = await AwaitSequenceCoreAsync(context, sequence).ConfigureAwait(false);

            if (!context.IsSequenceStart)
            {
                _logger.LogSequenceProcessingCompletedNotOuterStart(context.Envelope, sequence);
            }

            LogSequenceCompletedTrace(context, sequence);

            // Commit only if it is the start of the outer sequence (e.g., when combining batching and chunking), to avoid commits in the
            // middle of the outer sequence (batch).
            if (context.IsSequenceStart)
            {
                if (sequence.ParentSequence == null && !IsCompletingOrComplete(sequence))
                {
                    // The subscriber stopped enumerating before the end of the sequence (or the sequence is being aborted): abort it as
                    // the SequencerConsumerBehaviorBase does, or await the abort already in progress, instead of committing directly.
                    // This waits for the pending add, to commit the message being pushed when the enumeration stopped as well. The abort
                    // also commits and notifies the processing completion.
                    await sequence.AbortAsync(SequenceAbortReason.EnumerationAborted).ConfigureAwait(false);
                }
                else if (!sequence.IsAborted)
                {
                    await context.TransactionManager.CommitAsync().ConfigureAwait(false);

                    if (context.Sequence is ISequenceImplementation sequenceImpl)
                        sequenceImpl.NotifyProcessingCompleted();
                }
            }
        }
        catch (Exception exception)
        {
            failedSequence = GetFailedSequence(context, sequence, exception);
            await failedSequence.AbortAsync(StopCancellationHelper.GetSequenceAbortReason(exception, cancellationToken), exception)
                .ConfigureAwait(false);
        }
        finally
        {
            failedSequence?.Dispose();
            sequence.Dispose();
        }
    }

    private async Task<(ConsumerPipelineContext Context, ISequence Sequence)> AwaitSequenceCoreAsync(
        ConsumerPipelineContext context,
        ISequence sequence)
    {
        Task? processingTask = context.ProcessingTask;

        // Keep awaiting in a loop because the sequence and the processing task may be reassigned
        // (a ChunkSequence may be added to another sequence only after the message is complete and deserialized)
        while (processingTask != null)
        {
            _logger.LogSequenceProcessingTaskAwaiting(context.Envelope, sequence);

            await processingTask.ConfigureAwait(false);

            _logger.LogSequenceProcessingTaskAwaited(context.Envelope, sequence);

            // Break if this isn't the beginning of the outer sequence (e.g., when combining batching and chunking)
            if (!context.IsSequenceStart)
                return (context, sequence);

            sequence = context.Sequence ?? throw new InvalidOperationException("Sequence is null.");
            context = sequence.Context;

            processingTask = context.ProcessingTask != processingTask
                ? context.ProcessingTask
                : null;
        }

        return (context, sequence);
    }

    private void LogSequenceCompletedTrace(ConsumerPipelineContext context, ISequence sequence)
    {
        if (!context.IsSequenceStart)
        {
            _logger.LogSequenceProcessingCompletedNotOuterStart(context.Envelope, sequence);
        }

        if (sequence.IsPending)
        {
            _logger.LogSequenceStillPending(context.Envelope, sequence, context.ProcessingTask);
        }
        else
        {
            _logger.LogSequenceProcessingCompleted(context.Envelope, sequence);
        }
    }

    private async Task<bool> HandleSequenceExceptionAsync(
        ConsumerPipelineContext context,
        ISequence sequence,
        Exception exception,
        bool isSequenceProcessingAwaiterStarted,
        CancellationToken cancellationToken)
    {
        await sequence.AbortAsync(StopCancellationHelper.GetSequenceAbortReason(exception, cancellationToken), exception)
            .ConfigureAwait(false);

        if (sequence.Length > 0)
        {
            _logger.LogSequenceProcessingAwaitingBeforeRethrow(context.Envelope);
            await sequence.AwaitProcessingAsync(false).ConfigureAwait(false);
        }

        // The cancellation requested by the consumer stop isn't a processing failure: the sequence was rolled back without
        // applying the error policies. Rely on the abort reason, since the error policies applied by the abort might have
        // stopped the consumer, canceling the token in the meantime.
        bool isStopCancellation = StopCancellationHelper.IsStopCancellation(exception, sequence);

        // The processing takes place in the context of the first element, which is disposed with the sequence by the awaiter:
        // start it now if the sequence start failed, not to leak the sequence whether the exception is rethrown or not
        if (context.IsSequenceStart && !isSequenceProcessingAwaiterStarted)
            StartSequenceProcessingAwaiter(context, cancellationToken);

        if (!isStopCancellation)
            return false;

        // Rolled back to be consumed again
        if (!context.IsSequenceStart)
            context.Dispose();

        return true;
    }

    private async Task RollbackStopCancellationAsync(ConsumerPipelineContext context, Exception exception)
    {
        _logger.LogProcessingCanceledByConsumerStop(context.Envelope, exception);

        try
        {
            // The consumer is already being stopped: just roll back, so that the message is consumed again
            await context.TransactionManager.RollbackAsync(exception, stopConsuming: false).ConfigureAwait(false);
        }
        finally
        {
            context.Dispose();
        }
    }

    private async Task<bool> HandleExceptionAsync(ConsumerPipelineContext context, Exception exception)
    {
        _logger.LogProcessingError(context.Envelope, exception);

        try
        {
            bool handled = !IsHandledByUnboundedSequenceAbort(context, exception) &&
                           await ErrorPoliciesHelper.ApplyErrorPoliciesAsync(context, exception).ConfigureAwait(false);

            if (!handled)
            {
                if (context.Sequence != null && (context.Sequence.Context.ProcessingTask?.IsCompleted ?? true))
                    await context.Sequence.Context.TransactionManager.RollbackAsync(exception).ConfigureAwait(false);
                else
                    await context.TransactionManager.RollbackAsync(exception).ConfigureAwait(false);
            }

            return handled;
        }
        finally
        {
            context.Dispose();
        }
    }
}
