// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Behaviors
{
    /// <summary>
    ///     It ensures that an x-message-id header is always produced.
    /// </summary>
    public class MessageIdInitializerProducerBehavior : IProducerBehavior
    {
        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Producer.MessageIdInitializer;

        /// <inheritdoc cref="IProducerBehavior.HandleAsync" />
        public async Task HandleAsync(
            ProducerPipelineContext context,
            ProducerBehaviorHandler next,
            CancellationToken cancellationToken = default)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            MessageIdProvider.EnsureMessageIdIsInitialized(context.Envelope.Headers);

            await next(context, cancellationToken).ConfigureAwait(false);
        }
    }
}
