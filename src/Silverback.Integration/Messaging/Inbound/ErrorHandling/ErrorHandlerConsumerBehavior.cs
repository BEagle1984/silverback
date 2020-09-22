// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Inbound.ErrorHandling
{
    /// <summary>
    ///     Applies the error policies.
    /// </summary>
    public class ErrorHandlerConsumerBehavior : IConsumerBehavior
    {
        // TODO: Ensure instance per consumer
        private readonly ConcurrentDictionary<IOffset, int> _failedAttemptsCounters =
            new ConcurrentDictionary<IOffset, int>();

        // private readonly IErrorPolicyHelper _errorPolicyHelper;
        //
        // /// <summary>
        // ///     Initializes a new instance of the <see cref="ErrorHandlerConsumerBehavior" /> class.
        // /// </summary>
        // /// <param name="errorPolicyHelper">
        // ///     The <see cref="IErrorPolicyHelper" /> to be used to apply the defined error policies.
        // /// </param>
        // public ErrorHandlerConsumerBehavior(IErrorPolicyHelper errorPolicyHelper)
        // {
        //     _errorPolicyHelper = Check.NotNull(errorPolicyHelper, nameof(errorPolicyHelper));
        // }

        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.ErrorHandler;

        /// <inheritdoc cref="IConsumerBehavior.Handle" />
        public async Task Handle(
            ConsumerPipelineContext context,
            ConsumerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            // TODO:
            // * Invoke policies, passing consumer to perform the necessary action (seek, move, etc.)
            // * Handle rebalance during seek / move (don't crash)
            // * Count failed attempts
            // * Can probably move the logic in here and get rid of the helper?

            try
            {
                await next(context).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                SetFailedAttempts(context.Envelope);

                var handled = await context.Envelope.Endpoint.ErrorPolicy
                    .Build(context.ServiceProvider)
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
