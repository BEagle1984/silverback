// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Inbound.Transaction;
using Silverback.Util;

namespace Silverback.Messaging.Inbound
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
            context.TransactionManager = new ConsumerTransactionManager(context.Consumer);
            await next(context).ConfigureAwait(false);
        }
    }
}
