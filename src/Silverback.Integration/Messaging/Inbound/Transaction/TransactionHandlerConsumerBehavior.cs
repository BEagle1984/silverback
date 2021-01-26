// Copyright (c) 2020 Sergio Aquilini
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
        private readonly ISilverbackIntegrationLogger<TransactionHandlerConsumerBehavior> _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TransactionHandlerConsumerBehavior" /> class.
        /// </summary>
        /// <param name="logger">
        ///     The <see cref="ISilverbackIntegrationLogger{TCategoryName}" />.
        /// </param>
        public TransactionHandlerConsumerBehavior(
            ISilverbackIntegrationLogger<TransactionHandlerConsumerBehavior> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.TransactionHandler;

        /// <inheritdoc cref="IConsumerBehavior.HandleAsync" />
        [SuppressMessage("", "CA2000", Justification = "ServiceScope is disposed with the Context")]
        [SuppressMessage("", "CA1508", Justification = "False positive: is ISequenceImplementation")]
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
                        .GetRequiredService<ISilverbackIntegrationLogger<ConsumerTransactionManager>>());

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
                // Sequence errors are handled in AwaitSequenceProcessingAsync, just await the rollback and rethrow
                if (context.Sequence != null)
                {
                    await context.Sequence.AbortAsync(SequenceAbortReason.Error, exception)
                        .ConfigureAwait(false);

                    if (context.Sequence.Length > 0)
                    {
                        _logger.LogTraceWithMessageInfo(
                            IntegrationEventIds.LowLevelTracing,
                            "Awaiting sequence processing completed before rethrowing.",
                            context);

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
                _logger.LogTraceWithMessageInfo(
                    IntegrationEventIds.LowLevelTracing,
                    "Sequence ended or aborted: awaiting processing task.",
                    context);

                await context.Sequence.AwaitProcessingAsync(true).ConfigureAwait(false);

                _logger.LogTraceWithMessageInfo(
                    IntegrationEventIds.LowLevelTracing,
                    "Sequence ended or aborted: processing task completed.",
                    context);

                context.Dispose();
            }
        }

        [SuppressMessage("", "VSTHRD110", Justification = Justifications.FireAndForget)]
        private void StartSequenceProcessingAwaiter(ConsumerPipelineContext context) =>
            Task.Run(() => AwaitSequenceProcessingAsync(context));

        [SuppressMessage("", "CA1031", Justification = "Exception passed to AbortAsync")]
        private async Task AwaitSequenceProcessingAsync(ConsumerPipelineContext context)
        {
            var sequence = context.Sequence ??
                           throw new InvalidOperationException("Sequence is null.");
            context = sequence.Context;

            try
            {
                var processingTask = context.ProcessingTask;

                // Keep awaiting in a loop because the sequence and the processing task may be reassigned
                // (a ChunkSequence may be added to another sequence only after the message is complete and deserialized)
                while (processingTask != null)
                {
                    _logger.LogTraceWithMessageInfo(
                        IntegrationEventIds.LowLevelTracing,
                        $"Awaiting {sequence.GetType().Name} '{sequence.SequenceId}' processing task.",
                        context);

                    await processingTask.ConfigureAwait(false);

                    // Ensure we are at the start of the outer sequence
                    if (!context.IsSequenceStart)
                    {
                        _logger.LogTraceWithMessageInfo(
                            IntegrationEventIds.LowLevelTracing,
                            $"{sequence.GetType().Name} '{sequence.SequenceId}' processing has completed but it's not the beginning of the outer sequence. No action will be performed.",
                            context);
                        return;
                    }

                    sequence = context.Sequence ??
                               throw new InvalidOperationException("Sequence is null.");
                    context = sequence.Context;

                    processingTask = context.ProcessingTask != processingTask
                        ? context.ProcessingTask
                        : null;
                }

                string logMessage = sequence.IsPending
                    ? $"{sequence.GetType().Name} '{sequence.SequenceId}' processing seems " +
                      "completed but the sequence is still pending. " +
                      $"(ProcessingTask.Status={context.ProcessingTask?.Status}, ProcessingTask.Id={context.ProcessingTask?.Id})"
                    : $"{sequence.GetType().Name} '{sequence.SequenceId}' processing completed.";

                _logger.LogTraceWithMessageInfo(
                    IntegrationEventIds.LowLevelTracing,
                    logMessage,
                    context);

                if (!sequence.IsAborted)
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

        private async Task<bool> HandleExceptionAsync(
            ConsumerPipelineContext context,
            Exception exception)
        {
            _logger.LogProcessingError(context, exception);

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
