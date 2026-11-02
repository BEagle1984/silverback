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
                await context.Sequence.AbortAsync(StopCancellationHelper.GetSequenceAbortReason(exception, cancellationToken), exception)
                    .ConfigureAwait(false);

                if (context.Sequence.Length > 0)
                {
                    _logger.LogProcessingTrace(context.Envelope, "Awaiting sequence processing completed before rethrowing.");
                    await context.Sequence.AwaitProcessingAsync(false).ConfigureAwait(false);
                }

                // The cancellation requested by the consumer stop isn't a processing failure: the sequence was rolled back without
                // applying the error policies. Rely on the abort reason, since the error policies applied by the abort might have
                // stopped the consumer, canceling the token in the meantime.
                bool isStopCancellation = StopCancellationHelper.IsStopCancellation(exception, context.Sequence);

                // The processing takes place in the context of the first element, which is disposed with the sequence by the awaiter:
                // start it now if the sequence start failed, not to leak the sequence whether the exception is rethrown or not
                if (context.IsSequenceStart && !isSequenceProcessingAwaiterStarted)
                    StartSequenceProcessingAwaiter(context, cancellationToken);

                if (!isStopCancellation)
                    throw;

                // Rolled back to be consumed again
                if (!context.IsSequenceStart)
                    context.Dispose();

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

    // When streaming, the exception might come from the unbounded sequence processing, whose abort applied the error policies already
    // (possibly stopping the consumer): rely on its abort reason, like for the other sequences. Check the reason rather than IsAborted,
    // since the state is switched before the reason is set: until then rely on the token, which the error policies cancel only afterwards.
    private static bool IsStopCancellation(ConsumerPipelineContext context, Exception exception, CancellationToken cancellationToken) =>
        context.UnboundedSequence is { AbortReason: not SequenceAbortReason.None } unboundedSequence
            ? StopCancellationHelper.IsStopCancellation(exception, unboundedSequence)
            : StopCancellationHelper.IsStopCancellation(exception, cancellationToken);

    private async Task AwaitProcessedIfNecessaryAsync(ConsumerPipelineContext context)
    {
        if (context.Sequence == null)
            throw new InvalidOperationException("Sequence is null");

        // At the end of the sequence (or when the processing task exits prematurely), ensure that the
        // commit was performed or the error policies were applied before continuing
        if (context.IsSequenceEnd || context.Sequence.IsAborted)
        {
            _logger.LogProcessingTrace(context.Envelope, "Sequence ended or aborted: awaiting processing task.");

            try
            {
                await context.Sequence.AwaitProcessingAsync(true).ConfigureAwait(false);
            }
            finally
            {
                _logger.LogProcessingTrace(context.Envelope, "Sequence ended or aborted: processing task completed.");
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
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Logging is sync")]
    private async Task AwaitSequenceProcessingAsync(ConsumerPipelineContext context, CancellationToken cancellationToken)
    {
        ISequence sequence = context.Sequence ?? throw new InvalidOperationException("Sequence is null.");
        context = sequence.Context;

        try
        {
            (context, sequence) = await AwaitSequenceCoreAsync(context, sequence).ConfigureAwait(false);

            if (!context.IsSequenceStart)
            {
                _logger.LogProcessingTrace(
                    context.Envelope,
                    "{sequenceType} '{sequenceId}' processing has completed but it's not the beginning of the outer sequence. No action will be performed.",
                    () => [sequence.GetType().Name, sequence.SequenceId]);
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
            await sequence.AbortAsync(StopCancellationHelper.GetSequenceAbortReason(exception, cancellationToken), exception)
                .ConfigureAwait(false);
        }
        finally
        {
            sequence.Dispose();
        }
    }

    [SuppressMessage("ReSharper", "AccessToModifiedClosure", Justification = "Logging is sync")]
    private async Task<(ConsumerPipelineContext Context, ISequence Sequence)> AwaitSequenceCoreAsync(
        ConsumerPipelineContext context,
        ISequence sequence)
    {
        Task? processingTask = context.ProcessingTask;

        // Keep awaiting in a loop because the sequence and the processing task may be reassigned
        // (a ChunkSequence may be added to another sequence only after the message is complete and deserialized)
        while (processingTask != null)
        {
            _logger.LogProcessingTrace(
                context.Envelope,
                "Awaiting {sequenceType} '{sequenceId}' processing task.",
                () => [sequence.GetType().Name, sequence.SequenceId]);

            await processingTask.ConfigureAwait(false);

            _logger.LogProcessingTrace(
                context.Envelope,
                "Successfully awaited {sequenceType} '{sequenceId}' processing task.",
                () => [sequence.GetType().Name, sequence.SequenceId]);

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
            _logger.LogProcessingTrace(
                context.Envelope,
                "{sequenceType} '{sequenceId}' processing has completed but it's not the beginning of the outer sequence. No action will be performed.",
                () => [sequence.GetType().Name, sequence.SequenceId]);
        }

        if (sequence.IsPending)
        {
            _logger.LogProcessingTrace(
                context.Envelope,
                "{sequenceType} '{sequenceId}' processing seems completed but the sequence is still pending: ProcessingTask.Status={processingTaskStatus}, ProcessingTask.Id={processingTaskId})",
                () => [sequence.GetType().Name, sequence.SequenceId, context.ProcessingTask?.Status, context.ProcessingTask?.Id]);
        }
        else
        {
            _logger.LogProcessingTrace(
                context.Envelope,
                "{sequenceType} '{sequenceId}' processing completed.",
                () => [sequence.GetType().Name, sequence.SequenceId]);
        }
    }

    private async Task RollbackStopCancellationAsync(ConsumerPipelineContext context, Exception exception)
    {
        _logger.LogProcessingTrace(context.Envelope, exception, "Processing canceled by the consumer stop: rolling back the transaction.");

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
            bool handled = await ErrorPoliciesHelper.ApplyErrorPoliciesAsync(context, exception).ConfigureAwait(false);

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
