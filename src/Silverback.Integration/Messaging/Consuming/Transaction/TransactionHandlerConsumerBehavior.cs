// Copyright (c) 2025 Sergio Aquilini
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
                    StartSequenceProcessingAwaiter(context);

                await AwaitProcessedIfNecessaryAsync(context).ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            // Sequence errors are handled in AwaitSequenceProcessingAsync, just await the rollback and rethrow
            // (-> if the exception bubbled up till this point, it's because it failed to be handled, and it's safer to stop the consumer)
            if (context.Sequence != null)
            {
                await context.Sequence.AbortAsync(SequenceAbortReason.Error, exception).ConfigureAwait(false);

                if (context.Sequence.Length > 0)
                {
                    _logger.LogProcessingTrace(context.Envelope, "Awaiting sequence processing completed before rethrowing.");
                    await context.Sequence.AwaitProcessingAsync(false).ConfigureAwait(false);
                }

                throw;
            }

            if (!await HandleExceptionAsync(context, exception).ConfigureAwait(false))
                throw;
        }
    }

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

    private void StartSequenceProcessingAwaiter(ConsumerPipelineContext context) =>
        Task.Run(() => AwaitSequenceProcessingAsync(context)).FireAndForget();

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception passed to AbortAsync")]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Logging is sync")]
    private async Task AwaitSequenceProcessingAsync(ConsumerPipelineContext context)
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
            if (context.IsSequenceStart && !sequence.IsAborted)
            {
                await context.TransactionManager.CommitAsync().ConfigureAwait(false);

                if (context.Sequence is ISequenceImplementation sequenceImpl)
                    sequenceImpl.NotifyProcessingCompleted();
            }
        }
        catch (Exception exception)
        {
            await sequence.AbortAsync(SequenceAbortReason.Error, exception).ConfigureAwait(false);
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
