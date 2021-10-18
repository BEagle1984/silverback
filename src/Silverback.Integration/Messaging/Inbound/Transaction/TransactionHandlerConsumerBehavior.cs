﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Messaging.Sequences;
using Silverback.Util;

namespace Silverback.Messaging.Inbound.Transaction
{
    /// <summary>
    ///     Handles the consumer transaction and applies the error policies.
    /// </summary>
    public class TransactionHandlerConsumerBehavior : IConsumerBehavior
    {
        private readonly IInboundLogger<TransactionHandlerConsumerBehavior> _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TransactionHandlerConsumerBehavior" /> class.
        /// </summary>
        /// <param name="logger">
        ///     The <see cref="IInboundLogger{TCategoryName}" />.
        /// </param>
        public TransactionHandlerConsumerBehavior(IInboundLogger<TransactionHandlerConsumerBehavior> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.TransactionHandler;

        /// <inheritdoc cref="IConsumerBehavior.HandleAsync" />
        [SuppressMessage("", "CA2000", Justification = "ServiceScope is disposed with the Context")]
        public async Task HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            try
            {
                var scope = context.ServiceProvider.CreateScope();

                context.ReplaceServiceScope(scope);
                context.TransactionManager = new ConsumerTransactionManager(
                    context,
                    context.ServiceProvider
                        .GetRequiredService<IInboundLogger<ConsumerTransactionManager>>());

                _logger.LogProcessing(context.Envelope);

                await next(context).ConfigureAwait(false);

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
                // Sequence errors are handled in AwaitSequenceProcessingAsync, just await the rollback and
                // rethrow (-> if the exception bubbled up till this point, it's because it failed to be
                // handled and it's safer to stop the consumer)
                if (context.Sequence != null)
                {
                    await context.Sequence.AbortAsync(SequenceAbortReason.Error, exception)
                        .ConfigureAwait(false);

                    if (context.Sequence.Length > 0)
                    {
                        _logger.LogInboundLowLevelTrace(
                            "Awaiting sequence processing completed before rethrowing.",
                            context.Envelope);

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
                _logger.LogInboundLowLevelTrace(
                    "Sequence ended or aborted: awaiting processing task.",
                    context.Envelope);

                await context.Sequence.AwaitProcessingAsync(true).ConfigureAwait(false);

                _logger.LogInboundLowLevelTrace(
                    "Sequence ended or aborted: processing task completed.",
                    context.Envelope);

                context.Dispose();
            }
        }

        [SuppressMessage("", "VSTHRD110", Justification = Justifications.FireAndForget)]
        private void StartSequenceProcessingAwaiter(ConsumerPipelineContext context) =>
            Task.Run(() => AwaitSequenceProcessingAsync(context));

        [SuppressMessage("", "CA1031", Justification = "Exception passed to AbortAsync")]
        [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Logging is sync")]
        private async Task AwaitSequenceProcessingAsync(ConsumerPipelineContext context)
        {
            var sequence = context.Sequence ??
                           throw new InvalidOperationException("Sequence is null.");
            context = sequence.Context;

            try
            {
                (context, sequence) = await AwaitSequenceCoreAsync(context, sequence).ConfigureAwait(false);

                if (!context.IsSequenceStart)
                {
                    _logger.LogInboundLowLevelTrace(
                        "{sequenceType} '{sequenceId}' processing has completed but it's not the beginning of the outer sequence. No action will be performed.",
                        context.Envelope,
                        () => new object[]
                        {
                            sequence.GetType().Name,
                            sequence.SequenceId
                        });
                }

                LogSequenceCompletedTrace(context, sequence);

                // Commit only if it is the start of the outer sequence (e.g. when combining batching and
                // chunking), to avoid commits in the middle of the outer sequence (batch).
                if (context.IsSequenceStart && !sequence.IsAborted)
                {
                    await context.TransactionManager.CommitAsync().ConfigureAwait(false);

                    if (context.Sequence is ISequenceImplementation sequenceImpl)
                        sequenceImpl.NotifyProcessingCompleted();
                }
            }
            catch (Exception exception)
            {
                await sequence.AbortAsync(SequenceAbortReason.Error, exception)
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
            var processingTask = context.ProcessingTask;

            // Keep awaiting in a loop because the sequence and the processing task may be reassigned
            // (a ChunkSequence may be added to another sequence only after the message is complete and deserialized)
            while (processingTask != null)
            {
                _logger.LogInboundLowLevelTrace(
                    "Awaiting {sequenceType} '{sequenceId}' processing task.",
                    context.Envelope,
                    () => new object[]
                    {
                        sequence.GetType().Name,
                        sequence.SequenceId
                    });

                await processingTask.ConfigureAwait(false);

                // Break if this isn't the beginning of the outer sequence
                // (e.g. when combining batching and chunking)
                if (!context.IsSequenceStart)
                    return (context, sequence);

                sequence = context.Sequence ??
                           throw new InvalidOperationException("Sequence is null.");
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
                var logMessage =
                    "{sequenceType} '{sequenceId}' processing has completed but it's not the beginning " +
                    "of the outer sequence. No action will be performed.";

                _logger.LogInboundLowLevelTrace(
                    logMessage,
                    context.Envelope,
                    () => new object[]
                    {
                        sequence.GetType().Name,
                        sequence.SequenceId
                    });
            }

            if (sequence.IsPending)
            {
                var logMessage =
                    "{sequenceType} '{sequenceId}' processing seems completed but the sequence is " +
                    "still pending: " +
                    "ProcessingTask.Status={processingTaskStatus}, " +
                    "ProcessingTask.Id={processingTaskId})";

                _logger.LogInboundLowLevelTrace(
                    logMessage,
                    context.Envelope,
                    () => new object?[]
                    {
                        sequence.GetType().Name,
                        sequence.SequenceId,
                        context.ProcessingTask?.Status,
                        context.ProcessingTask?.Id
                    });
            }
            else
            {
                _logger.LogInboundLowLevelTrace(
                    "{sequenceType} '{sequenceId}' processing completed.",
                    context.Envelope,
                    () => new object[]
                    {
                        sequence.GetType().Name,
                        sequence.SequenceId
                    });
            }
        }

        private async Task<bool> HandleExceptionAsync(
            ConsumerPipelineContext context,
            Exception exception)
        {
            _logger.LogProcessingError(context.Envelope, exception);

            try
            {
                bool handled = await ErrorPoliciesHelper.ApplyErrorPoliciesAsync(context, exception)
                    .ConfigureAwait(false);

                if (!handled)
                {
                    if (context.Sequence != null &&
                        (context.Sequence.Context.ProcessingTask?.IsCompleted ?? true))
                    {
                        await context.Sequence.Context.TransactionManager.RollbackAsync(exception)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        await context.TransactionManager.RollbackAsync(exception)
                            .ConfigureAwait(false);
                    }
                }

                return handled;
            }
            finally
            {
                context.Dispose();
            }
        }
    }
}
