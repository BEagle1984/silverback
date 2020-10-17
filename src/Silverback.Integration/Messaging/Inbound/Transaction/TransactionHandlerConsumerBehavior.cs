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
        [SuppressMessage("", "CA2000", Justification = "ServiceScope is disposed while disposing the Context")]
        public async Task HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next)
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
                else if (context.IsSequenceStart)
                {
                    StartSequenceProcessingAwaiter(context);
                }
                else
                {
                    if (context.IsSequenceEnd)
                    {
                        // Ensure that the commit or rollback was performed before continuing
                        if (context.Sequence is ISequenceImplementation sequenceImpl)
                            await sequenceImpl.ProcessedTaskCompletionSource.Task.ConfigureAwait(false);
                    }

                    context.Dispose();
                }
            }
            catch (Exception exception)
            {
                // Sequence errors are handled in AwaitSequenceProcessingAsync, just await the rollback and rethrow
                if (context.Sequence != null)
                {
                    if (context.Sequence.Length > 0 && context.Sequence is ISequenceImplementation sequenceImpl)
                        await sequenceImpl.ProcessedTaskCompletionSource.Task.ConfigureAwait(false);

                    throw;
                }

                if (!await HandleExceptionAsync(context, exception).ConfigureAwait(false))
                    throw;
            }
        }

        private static void StartSequenceProcessingAwaiter(ConsumerPipelineContext context)
        {
#pragma warning disable 4014
            // ReSharper disable AccessToDisposedClosure
            Task.Run(() => AwaitSequenceProcessingAsync(context));

            // ReSharper restore AccessToDisposedClosure
#pragma warning restore 4014

            // TODO: Cleanup

            // If the sequence completed (or the associated processing task completed)
            // if (sequence.IsComplete || (sequence.Context.ProcessingTask?.IsCompleted ?? false))
            // {
            //     await sequence.ProcessedTaskCompletionSource.Task.ConfigureAwait(false);
            //     sequence.Dispose();
            //     context.Dispose();
            // }
            //
            // if (context != sequence.Context)
            //     context.Dispose();
        }

        [SuppressMessage("", "CA1031", Justification = "Exception passed to AbortAsync to be logged and forwarded.")]
        private static async Task AwaitSequenceProcessingAsync(ConsumerPipelineContext context)
        {
            var sequence = context.Sequence ?? throw new InvalidOperationException("Sequence is null.");
            context = sequence.Context;

            try
            {
                // Keep awaiting in a loop because the sequence and the processing task may be reassigned
                while (context.ProcessingTask != null && !context.ProcessingTask.IsCompleted)
                {
                    await context.ProcessingTask.ConfigureAwait(false);

                    sequence = context.Sequence ?? throw new InvalidOperationException("Sequence is null.");
                    context = sequence.Context;
                }

                if (!sequence.IsAborted && !context.SequenceStore.HasPendingSequences)
                {
                    await context.TransactionManager.CommitAsync().ConfigureAwait(false);

                    if (context.Sequence is ISequenceImplementation sequenceImpl)
                        sequenceImpl.ProcessedTaskCompletionSource.SetResult(true);
                }
            }
            catch (Exception exception)
            {
                await context.Sequence.AbortAsync(SequenceAbortReason.Error, exception).ConfigureAwait(false);
            }
            finally
            {
                context.Sequence.Dispose();
            }
        }

        private async Task<bool> HandleExceptionAsync(ConsumerPipelineContext context, Exception exception)
        {
            _logger.LogProcessingError(context.Envelope, exception);

            try
            {
                bool handled = await ErrorPoliciesHelper.ApplyErrorPoliciesAsync(context, exception)
                    .ConfigureAwait(false);

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
    }
}
