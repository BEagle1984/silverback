// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Consuming.KafkaOffsetStore;

/// <summary>
///     Creates the <see cref="KafkaOffsetStoreScope" /> and ensures that the offsets are being stored to the client side offset store
///     (if configured).
/// </summary>
public class KafkaOffsetStoreConsumerBehavior : IConsumerBehavior
{
    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Publisher - 10;

    /// <inheritdoc cref="IConsumerBehavior.HandleAsync" />
    public ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next, CancellationToken cancellationToken)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        if (context.Consumer is KafkaConsumer { Configuration.ClientSideOffsetStore: not null } kafkaConsumer)
        {
            context.ServiceProvider
                .GetRequiredService<SilverbackContext>()
                .SetKafkaOffsetStoreScope(CreateOffsetStoreScope(context, kafkaConsumer.Configuration.ClientSideOffsetStore));
            context.TransactionManager.Committing.AddHandler(CommitOffsetsAsync);
        }

        return next(context, cancellationToken);
    }

    private static KafkaOffsetStoreScope CreateOffsetStoreScope(ConsumerPipelineContext context, KafkaOffsetStoreSettings storeSettings) =>
        new(
            context.ServiceProvider.GetRequiredService<IKafkaOffsetStoreFactory>().GetStore(storeSettings, context.ServiceProvider),
            context);

    private static ValueTask CommitOffsetsAsync(ConsumerPipelineContext context) =>
        new(context.ServiceProvider.GetRequiredService<SilverbackContext>().GetKafkaOffsetStoreScope().StoreOffsetsAsync());
}
