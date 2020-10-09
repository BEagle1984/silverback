// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.Inbound
{
    /// <summary>
    ///     Publishes the consumed messages to the internal bus.
    /// </summary>
    public class PublisherConsumerBehavior : IConsumerBehavior
    {
        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Publisher;

        /// <inheritdoc cref="IConsumerBehavior.Handle" />
        public async Task Handle(
            ConsumerPipelineContext context,
            ConsumerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            var publisher = context.ServiceProvider.GetRequiredService<IPublisher>();

            await publisher.PublishAsync(context.Envelope, context.Envelope.Endpoint.ThrowIfUnhandled).ConfigureAwait(false);

            // TODO: Publish stream(s) (not sequences) -> will need async thread

            await next(context).ConfigureAwait(false);
        }
    }
}
