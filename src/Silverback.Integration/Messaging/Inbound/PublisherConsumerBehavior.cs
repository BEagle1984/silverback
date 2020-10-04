// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
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
            try
            {
                Check.NotNull(context, nameof(context));
                Check.NotNull(next, nameof(next));

                var publisher = context.ServiceProvider.GetRequiredService<IPublisher>();

                // TODO: Worth optimizing if not a sequence? (I don't think so)
                // TODO: Task.Run is needed to avoid all locks? (think about sync subscribers)
                context.ProcessingTask = Task.Run(() => publisher.PublishAsync(context.Envelope));

                // TODO: Publish stream(s) (not sequences) -> will need async thread

                await next(context).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // TODO: ???
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
