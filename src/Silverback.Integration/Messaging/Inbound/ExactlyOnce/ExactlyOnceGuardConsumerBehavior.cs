// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Headers;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.ExactlyOnce
{
    /// <summary>
    ///     Uses the configured implementation of <see cref="IXy"/> to ensure that the message is processed only once.
    /// </summary>
    public class ExactlyOnceGuardConsumerBehavior : IConsumerBehavior
    {
        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.ExactlyOnceGuard;

        /// <inheritdoc cref="IConsumerBehavior.Handle" />
        public async Task Handle(
            ConsumerPipelineContext context,
            ConsumerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            // TODO: Use strategy to determine if the message has to be skipped

            await next(context).ConfigureAwait(false);
        }
    }
}
