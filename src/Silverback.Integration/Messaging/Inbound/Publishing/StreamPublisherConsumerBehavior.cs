// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Inbound.Publishing
{
    /// <summary>
    ///     Publishes the stream containing the consumed messages.
    /// </summary>
    public class StreamPublisherConsumerBehavior : IConsumerBehavior
    {
        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.StreamPublisher;

        /// <inheritdoc cref="IConsumerBehavior.Handle" />
        public async Task Handle(
            ConsumerPipelineContext context,
            ConsumerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            // TODO:
            // * Create stream per each consumer
            // * Push all messages into it
            // * Design sequencer / chunkaggregator
            // * Move to Streaming namespace

            await next(context).ConfigureAwait(false);
        }
    }
}
