// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

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
    public async ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        if (context.Consumer is not KafkaConsumer kafkaConsumer || kafkaConsumer.Configuration.ClientSideOffsetStore == null)
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        IKafkaOffsetStoreFactory offsetStoreFactory = context.ServiceProvider.GetRequiredService<IKafkaOffsetStoreFactory>();
        IKafkaOffsetStore offsetStore = offsetStoreFactory.GetStore(kafkaConsumer.Configuration.ClientSideOffsetStore);
        KafkaOffsetStoreScope offsetStoreScope = new(offsetStore, context);

        SilverbackContext silverbackContext = context.ServiceProvider.GetRequiredService<SilverbackContext>();
        silverbackContext.SetKafkaOffsetStoreScope(offsetStoreScope);

        await next(context).ConfigureAwait(false);

        await offsetStoreScope.StoreOffsetsAsync().ConfigureAwait(false);
    }
}
