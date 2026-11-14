// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
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
                context.ServiceProvider.GetRequiredService<ISilverbackLogger<ConsumerTransactionManager>>(),
                cancellationToken);
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

    // The failure of the outer sequence the message is added to (e.g., the batch a chunked message is added to by the processing of its
    // chunk sequence), while the inner sequence is still referenced, is handled by aborting the outer sequence, unless it was aborted for a
    // reason that doesn't handle failures (e.g., the subscriber stopped enumerating it and its commit failed), applying the error policies
    // to the inner sequence then:
    // - Any failure of the message that joined the outer sequence is a failure of the outer sequence, as for the messages pushed directly
    //   to it (see HandleSequenceExceptionAsync), whatever the exception (e.g., the failure pushing the message to the batch, or completing
    //   the batch whose streams were aborted).
    // - When the context started the aborted outer sequence as well, any failure belongs to it, whichever processing task threw it (e.g.,
    //   the cancellation thrown by the subscriber when the batch was aborted with another exception).
    // - The failure of an outer sequence is also rethrown to the message left out of it (e.g., refused because the batch timed out, see
    //   SequenceBase.AddAsync): it's either the exception that aborted the outer sequence or the one thrown by its error handling.
    // Otherwise the exception comes from the inner sequence processing (e.g., failing before the message is added to the outer sequence).
    private static bool IsOuterSequenceFailure(
        ConsumerPipelineContext context,
        ISequence sequence,
        Exception exception,
        [NotNullWhen(true)] out ISequence? outerSequence)
    {
        outerSequence = context.Sequence;

        if (outerSequence == null || outerSequence == sequence || outerSequence.IsAborted && !IsAbortedByErrorOrStop(outerSequence))
            return false;

        if (sequence.ParentSequence == outerSequence || context.IsSequenceStart && outerSequence.IsAborted)
            return true;

        return outerSequence.AbortException == exception || GetProcessingException(outerSequence) == exception;
    }

    // The abort handles the failures of the sequence, applying the error policies or rolling it back because the consumer is stopped,
    // unlike when aborted because incomplete or because the subscribers stopped enumerating it
    private static bool IsAbortedByErrorOrStop(ISequence sequence) =>
        sequence.IsAborted &&
        sequence.AbortReason is SequenceAbortReason.Error or SequenceAbortReason.ConsumerAborted or SequenceAbortReason.Disposing;

    private static Exception? GetProcessingException(ISequence sequence) =>
        sequence is ISequenceImplementation { ProcessingCompletedTask: { IsFaulted: true } processingCompletedTask }
            ? processingCompletedTask.Exception?.InnerException
            : null;

    // The abort of the outer sequence handles the failure, applying the error policies (unless aborted by the stop): await it (or abort it,
    // when the message failed to be pushed to it), since aborting the inner sequence as failed would apply them a second time. The inner
    // sequence is completed with the outer sequence it joined. When the message was left out instead, nothing else completes the inner
    // sequence, which may be complete already and stays in the store, where the stop and the next messages await its processing: abort it
    // like the stop aborts the pending sequences, rolling it back without applying the error policies (see SequencerConsumerBehaviorBase).
    private static async Task HandleOuterSequenceFailureAsync(
        ISequence sequence,
        ISequence outerSequence,
        SequenceAbortReason abortReason,
        Exception exception)
    {
        try
        {
            await outerSequence.AbortAsync(abortReason, exception).ConfigureAwait(false);
        }
        finally
        {
            if (sequence.ParentSequence != outerSequence)
                await sequence.AbortAsync(SequenceAbortReason.ConsumerAborted, exception).ConfigureAwait(false);
        }
    }

    // Awaits the subscribers of the sequence before it's disposed, which might still be processing its streams (e.g., when the message
    // failed to be pushed to it)
    private static async Task AwaitSubscribersAsync(ISequence sequence)
    {
        if (sequence.Context.ProcessingTask is { } processingTask)
            await processingTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }

    // The error handling of the failed sequence stopped the consumer, because the error policies don't handle the error or the error
    // handling failed (see ConsumerTransactionManager.TryStopConsumerAsync, which ignores the stop requested by a stale processing): the
    // client must be disconnected as well, as when the exception reaches the reader (see ConsumerChannelsManager), but no reader rethrows
    // it when no reader awaits the sequence (e.g., a batch completed by its max wait time, or ended by a chunked message, which the
    // processing of its chunk sequence adds)
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Logged by DisconnectAsync")]
    private static void DisconnectIfStoppedByErrorHandling(ISequence sequence, ISequence? ownedOuterSequence)
    {
        ConsumerTransactionManager? transactionManager = GetTransactionManagerIfStoppedByErrorHandling(sequence) ??
                                                         (ownedOuterSequence != null
                                                             ? GetTransactionManagerIfStoppedByErrorHandling(ownedOuterSequence)
                                                             : null);

        if (transactionManager == null)
            return;

        IConsumer consumer = sequence.Context.Consumer;
        int? stoppedConsumerStartGeneration = transactionManager.StoppedConsumerStartGeneration;

        // On another thread, since the disconnection waits for the readers to stop, while this continuation might run synchronously in the
        // abort of the sequence, which a reader might be waiting for. The consumer restarted after the stop isn't disconnected, unless
        // the stop failed (and the start generation is unknown).
        Task.Run(
                async () =>
                {
                    try
                    {
                        if (consumer is IStopOnFailureConsumer stopOnFailureConsumer && stoppedConsumerStartGeneration != null)
                        {
                            await stopOnFailureConsumer.DisconnectAfterStopOnFailureAsync(stoppedConsumerStartGeneration.Value)
                                .ConfigureAwait(false);
                        }
                        else
                        {
                            await consumer.Client.DisconnectAsync().ConfigureAwait(false);
                        }
                    }
                    catch (Exception)
                    {
                        // Logged by DisconnectAsync
                    }
                },
                CancellationToken.None)
            .FireAndForget();
    }

    // The sequence was aborted because of an error and its error handling stopped the consumer (its processing might have been completed
    // already, e.g., when it's a chunk sequence added to a batch, which completes it, failing afterwards)
    private static ConsumerTransactionManager? GetTransactionManagerIfStoppedByErrorHandling(ISequence sequence) =>
        sequence.AbortReason == SequenceAbortReason.Error &&
        sequence.Context.TransactionManager is ConsumerTransactionManager { IsConsumerStopped: true } transactionManager
            ? transactionManager
            : null;

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

    // The token is only used to recognize the cancellation requested by the consumer stop, the awaiter must run until completion. The
    // sequence and its processing task are captured before the awaiter runs, since the message might be added to an outer sequence in
    // the meantime (e.g., the chunked message added to a batch, which replaces the processing task of the context): the processing task of
    // the sequence must be awaited first, not to miss its failure.
    private void StartSequenceProcessingAwaiter(ConsumerPipelineContext context, CancellationToken cancellationToken)
    {
        Task? processingTask = context.ProcessingTask;
        ISequence sequence = context.Sequence ?? throw new InvalidOperationException("Sequence is null.");
        Task.Run(() => AwaitSequenceProcessingAsync(sequence, processingTask, cancellationToken), CancellationToken.None).FireAndForget();
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception passed to AbortAsync")]
    private async Task AwaitSequenceProcessingAsync(ISequence sequence, Task? processingTask, CancellationToken cancellationToken)
    {
        ConsumerPipelineContext context = sequence.Context;

        // The outer sequence started by the context (e.g., the batch started by a chunked message), which this awaiter owns
        ISequence? ownedOuterSequence = null;

        try
        {
            (context, ISequence processedSequence) =
                await AwaitSequenceCoreAsync(context, sequence, processingTask).ConfigureAwait(false);

            if (processedSequence != sequence)
                ownedOuterSequence = processedSequence;

            if (!context.IsSequenceStart)
            {
                _logger.LogSequenceProcessingCompletedNotOuterStart(context.Envelope, processedSequence);
            }

            LogSequenceCompletedTrace(context, processedSequence);

            // Commit only if it is the start of the outer sequence (e.g., when combining batching and chunking), to avoid commits in the
            // middle of the outer sequence (batch).
            if (context.IsSequenceStart)
            {
                if (processedSequence.ParentSequence == null && !IsCompletingOrComplete(processedSequence))
                {
                    // The subscriber stopped enumerating before the end of the sequence (or the sequence is being aborted): abort it as
                    // the SequencerConsumerBehaviorBase does, or await the abort already in progress, instead of committing directly.
                    // This waits for the pending add, to commit the message being pushed when the enumeration stopped as well. The abort
                    // also commits and notifies the processing completion.
                    await processedSequence.AbortAsync(SequenceAbortReason.EnumerationAborted).ConfigureAwait(false);
                }
                else if (!processedSequence.IsAborted)
                {
                    await context.TransactionManager.CommitAsync().ConfigureAwait(false);

                    if (context.Sequence is ISequenceImplementation sequenceImpl)
                        sequenceImpl.NotifyProcessingCompleted();
                }
            }
        }
        catch (Exception exception)
        {
            SequenceAbortReason abortReason = StopCancellationHelper.GetSequenceAbortReason(exception, cancellationToken);

            if (IsOuterSequenceFailure(context, sequence, exception, out ISequence? outerSequence))
            {
                if (context.IsSequenceStart)
                    ownedOuterSequence = outerSequence;

                await HandleOuterSequenceFailureAsync(sequence, outerSequence, abortReason, exception).ConfigureAwait(false);

                if (ownedOuterSequence != null)
                    await AwaitSubscribersAsync(ownedOuterSequence).ConfigureAwait(false);
            }
            else
            {
                await sequence.AbortAsync(abortReason, exception).ConfigureAwait(false);

                // The outer sequence started by the context, which the message didn't join (e.g., failing to publish it), would be left
                // pending otherwise: abort it as well, rolling it back without applying the error policies a second time
                if (context is { IsSequenceStart: true, Sequence: { } startedSequence } && startedSequence != sequence)
                {
                    ownedOuterSequence = startedSequence;
                    await startedSequence.AbortAsync(SequenceAbortReason.ConsumerAborted, exception).ConfigureAwait(false);
                    await AwaitSubscribersAsync(startedSequence).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            try
            {
                // Before disposing the sequences, which might throw (e.g., when a scoped service fails to dispose)
                DisconnectIfStoppedByErrorHandling(sequence, ownedOuterSequence);
            }
            finally
            {
                try
                {
                    ownedOuterSequence?.Dispose();
                }
                finally
                {
                    sequence.Dispose();
                }
            }
        }
    }

    private async Task<(ConsumerPipelineContext Context, ISequence Sequence)> AwaitSequenceCoreAsync(
        ConsumerPipelineContext context,
        ISequence sequence,
        Task? processingTask)
    {
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
