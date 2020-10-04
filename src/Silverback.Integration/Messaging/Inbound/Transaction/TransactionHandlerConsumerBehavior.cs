// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
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
                await next(context).ConfigureAwait(false);
                await context.TransactionManager.Commit().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                SetFailedAttempts(context.Envelope);

                var errorPolicyImplementation = context.Envelope.Endpoint.ErrorPolicy.Build(context.ServiceProvider);

                if (!errorPolicyImplementation.CanHandle(context, exception))
                    throw;

                var handled = await errorPolicyImplementation
                    .HandleError(context, exception)
                    .ConfigureAwait(false);

                if (!handled)
                    throw;
            }
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
