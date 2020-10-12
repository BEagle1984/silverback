// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
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

        // TODO: Ensure instance per consumer
        private readonly ConcurrentDictionary<IOffset, int> _failedAttemptsCounters =
            new ConcurrentDictionary<IOffset, int>();

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

        /// <inheritdoc cref="IConsumerBehavior.Handle" />
        [SuppressMessage("", "CA2000", Justification = "ServiceScope is disposed while disposing the Context")]
        public async Task Handle(ConsumerPipelineContext context, ConsumerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            try
            {
                // TODO: Ensure always disposed (TEST IT!)
                var scope = context.ServiceProvider.CreateScope();

                context.ReplaceServiceScope(scope);
                context.TransactionManager = new ConsumerTransactionManager(context);

                await next(context).ConfigureAwait(false);

                if (context.Sequence == null)
                {
                    await context.TransactionManager.CommitAsync().ConfigureAwait(false);
                    context.Dispose();
                }
                else
                {
                    await HandleSequenceAsync(context, context.Sequence).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                // Sequence errors are handled in the parallel thread
                if (context.Sequence != null)
                {
                    if (context.Sequence.Length > 0)
                        await context.Sequence.ProcessedTaskCompletionSource.Task.ConfigureAwait(false);

                    throw;
                }

                if (!await HandleExceptionAsync(context, exception).ConfigureAwait(false))
                    throw;
            }
        }

        private async Task HandleSequenceAsync(ConsumerPipelineContext context, ISequence sequence)
        {
            // This is the first message in the sequence, start another thread to await the sequence completion
            // and perform the commit or rollback
            if (context == sequence.Context)
            {
#pragma warning disable 4014
                // ReSharper disable AccessToDisposedClosure
                Task.Run(() => AwaitProcessingTaskAndCommitAsync(context, sequence));

                // ReSharper restore AccessToDisposedClosure
#pragma warning restore 4014
            }

            // If the sequence completed (or the associated processing task completed) wait for the the
            // asynchronous thread to perform the commit or rollback before continuing.
            if (sequence.IsComplete || (sequence.Context.ProcessingTask?.IsCompleted ?? false))
            {
                await sequence.ProcessedTaskCompletionSource.Task.ConfigureAwait(false);
                sequence.Dispose();
                context.Dispose();
            }

            if (context != sequence.Context)
                context.Dispose();
        }

        private async Task AwaitProcessingTaskAndCommitAsync(ConsumerPipelineContext context, ISequence sequence)
        {
            try
            {
                if (context.ProcessingTask != null)
                    await context.ProcessingTask.ConfigureAwait(false);

                if (!context.SequenceStore.HasPendingSequences)
                    await context.TransactionManager.CommitAsync().ConfigureAwait(false);

                sequence.ProcessedTaskCompletionSource.SetResult(true);
            }
            catch (Exception exception)
            {
                await HandleSequenceExceptionAsync(context, sequence, exception).ConfigureAwait(false);
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
                bool handled = await ApplyErrorPoliciesAsync(context, exception).ConfigureAwait(false);

                // TODO: Carefully test: exception handled once and always rolled back

                if (!handled)
                {
                    if (context.Sequence != null && (context.Sequence.Context.ProcessingTask?.IsCompleted ?? true))
                    {
                        await context.Sequence.Context.TransactionManager.RollbackAsync(exception)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        await context.TransactionManager.RollbackAsync(exception).ConfigureAwait(false);
                    }
                }

                return handled;
            }
            finally
            {
                context.Dispose();
            }
        }

        private async Task HandleSequenceExceptionAsync(
            ConsumerPipelineContext context,
            ISequence sequence,
            Exception exception)
        {
            _logger.LogProcessingError(context.Envelope, exception);

            try
            {
                // TODO: Must log?

                switch (sequence.AbortReason)
                {
                    case SequenceAbortReason.None:
                    case SequenceAbortReason.Error:
                        if (!await ApplyErrorPoliciesAsync(context, exception).ConfigureAwait(false))
                        {
                            await context.TransactionManager.RollbackAsync(exception).ConfigureAwait(false);

                            sequence.ProcessedTaskCompletionSource.SetException(exception);
                            return;
                        }

                        break;
                    case SequenceAbortReason.EnumerationAborted:
                        await context.TransactionManager.CommitAsync().ConfigureAwait(false);
                        break;
                    case SequenceAbortReason.IncompleteSequence:
                        await context.TransactionManager.RollbackAsync(exception, true).ConfigureAwait(false);
                        break;
                    case SequenceAbortReason.ConsumerAborted:
                    case SequenceAbortReason.Disposing:
                    default:
                        await context.TransactionManager.RollbackAsync(exception).ConfigureAwait(false);
                        break;
                }

                sequence.ProcessedTaskCompletionSource.SetResult(false);
            }
            catch (Exception ex)
            {
                sequence.ProcessedTaskCompletionSource.SetException(ex);
            }
        }

        private async Task<bool> ApplyErrorPoliciesAsync(ConsumerPipelineContext context, Exception exception)
        {
            SetFailedAttempts(context.Envelope);

            var errorPolicyImplementation = context.Envelope.Endpoint.ErrorPolicy.Build(context.ServiceProvider);

            if (!errorPolicyImplementation.CanHandle(context, exception))
                return false;

            return await errorPolicyImplementation
                .HandleError(context, exception)
                .ConfigureAwait(false);
        }

        private void SetFailedAttempts(IRawInboundEnvelope envelope) =>
            envelope.Headers.AddOrReplace(
                DefaultMessageHeaders.FailedAttempts,
                _failedAttemptsCounters.AddOrUpdate(
                    envelope.Offset,
                    _ => envelope.Headers.GetValueOrDefault<int>(DefaultMessageHeaders.FailedAttempts) + 1,
                    (_, count) => count + 1));
    }
}
