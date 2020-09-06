// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Behaviors
{
    /// <summary>
    ///     Ensures that a new <see cref="IServiceScope" /> is created to process each consumed message.
    /// </summary>
    public class ServiceScopeFactoryConsumerBehavior : IConsumerBehavior
    {
        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.ServiceScopeFactory;

        /// <inheritdoc cref="IConsumerBehavior.Handle" />
        public async Task Handle(
            ConsumerPipelineContext context,
            ConsumerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            using var scope = context.ServiceProvider.CreateScope();

            context.ServiceProvider = scope.ServiceProvider;
            await next(context).ConfigureAwait(false);
        }
    }

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

            await context.ServiceProvider.GetRequiredService<IPublisher>().PublishAsync(context.Envelope)
                .ConfigureAwait(false);

            await next(context).ConfigureAwait(false);
        }
    }

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
            // * Design sequencer / chunker
            // * Move to Streaming namespace

            await context.ServiceProvider.GetRequiredService<IPublisher>().PublishAsync(context.Envelope)
                .ConfigureAwait(false);

            await next(context).ConfigureAwait(false);
        }
    }
}
