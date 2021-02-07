// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Inbound.ExactlyOnce.Repositories;
using Silverback.Util;

namespace Silverback.Messaging.Inbound.ExactlyOnce
{
    /// <summary>
    ///     Uses an <see cref="IOffsetStore" /> to keep track of the latest processed offsets and guarantee that
    ///     each message is processed only once.
    /// </summary>
    public class OffsetStoreExactlyOnceStrategy : IExactlyOnceStrategy
    {
        /// <inheritdoc cref="IExactlyOnceStrategy.Build" />
        public IExactlyOnceStrategyImplementation Build(IServiceProvider serviceProvider) =>
            new OffsetStoreExactlyOnceStrategyImplementation(serviceProvider.GetRequiredService<IOffsetStore>());

        private class OffsetStoreExactlyOnceStrategyImplementation : IExactlyOnceStrategyImplementation
        {
            private readonly IOffsetStore _offsetStore;

            public OffsetStoreExactlyOnceStrategyImplementation(IOffsetStore offsetStore)
            {
                _offsetStore = offsetStore;
            }

            public async Task<bool> CheckIsAlreadyProcessedAsync(ConsumerPipelineContext context)
            {
                Check.NotNull(context, nameof(context));

                var envelope = context.Envelope;

                if (envelope.BrokerMessageIdentifier is not IBrokerMessageOffset offset)
                {
                    throw new InvalidOperationException(
                        "The message broker implementation doesn't seem to support comparable offsets. " +
                        "The OffsetStoreExactlyOnceStrategy cannot be used, please resort to LogExactlyOnceStrategy " +
                        "to ensure exactly-once processing.");
                }

                var latestOffset = await _offsetStore.GetLatestValueAsync(
                        envelope.BrokerMessageIdentifier.Key,
                        envelope.Endpoint)
                    .ConfigureAwait(false);

                if (latestOffset != null && latestOffset.CompareTo(offset) >= 0)
                    return true;

                context.TransactionManager.Enlist(_offsetStore);

                await _offsetStore.StoreAsync(offset, envelope.Endpoint).ConfigureAwait(false);
                return false;
            }
        }
    }
}
