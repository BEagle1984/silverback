// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Inbound.Transaction
{
    /// <summary>
    ///     Handles the consumer transaction and applies the error policies.
    /// </summary>
    public class TransactionHandlerConsumerBehavior : IConsumerBehavior
    {
        // TODO: Ensure instance per consumer
        private readonly ConcurrentDictionary<IOffset, int> _failedAttemptsCounters =
            new ConcurrentDictionary<IOffset, int>();

        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.TransactionHandler;

        /// <inheritdoc cref="IConsumerBehavior.Handle" />
        public async Task Handle(
            ConsumerPipelineContext context,
            ConsumerBehaviorHandler next)
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
                    await AwaitProcessingAndCommit(context).ConfigureAwait(false);
                else if (context.Sequence.IsComplete || (context.ProcessingTask?.IsCompleted ?? false))
                    await AwaitProcessingAndCommit(context.Sequence.Context).ConfigureAwait(false);
                else if (context != context.Sequence.Context)
                    context.Dispose();
            }
            catch (Exception exception)
            {
                context.Dispose();
                context.Sequence?.Context.Dispose();

                if (!await HandleException(context, exception).ConfigureAwait(false))
                    throw;
            }
        }

        private async Task AwaitProcessingAndCommit(ConsumerPipelineContext context)
        {
            try
            {
                if (context.ProcessingTask != null)
                    await context.ProcessingTask.ConfigureAwait(false);

                if (!context.Consumer.SequenceStore.HasPendingSequences)
                    await context.TransactionManager.Commit().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                if (!await HandleException(context, exception).ConfigureAwait(false))
                    throw;
            }
            finally
            {
                context.Dispose();
            }
        }

        private async Task<bool> HandleException(ConsumerPipelineContext context, Exception exception)
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
