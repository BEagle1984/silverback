// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.Inbound
{
    /// <summary>
    ///     Starts the <see cref="Task"/> that will process the consumed message and initializes the <see cref="ConsumerPipelineContext.ProcessingTask"/> property. In case of a sequence the <see cref="Task"/> will complete only when the entire sequence has been processed (or the processing is aborted).
    /// </summary>
    public class ProcessingTaskStarterConsumerBehavior : IConsumerBehavior
    {
        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.ProcessingTaskStarter;

        /// <inheritdoc cref="IConsumerBehavior.Handle" />
        public Task Handle(
            ConsumerPipelineContext context,
            ConsumerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            // TODO: Worth optimizing if not a sequence? (I don't think so)
            // TODO: Task.Run is needed to avoid all locks? (think about sync subscribers)
            context.ProcessingTask = Task.Run(async () => await next(context).ConfigureAwait(false));

            return Task.CompletedTask;
        }
    }
}
