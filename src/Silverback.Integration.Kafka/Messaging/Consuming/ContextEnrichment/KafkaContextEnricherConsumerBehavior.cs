// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Util;

namespace Silverback.Messaging.Consuming.ContextEnrichment;

internal class KafkaContextEnricherConsumerBehavior : IConsumerBehavior
{
    public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.TransactionHandler + 1;

    public async ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        if (context is { Consumer: KafkaConsumer kafkaConsumer, Envelope.BrokerMessageIdentifier: KafkaOffset offset })
        {
            context.ServiceProvider.GetRequiredService<SilverbackContext>().SetConsumedPartition(
                offset.TopicPartition,
                kafkaConsumer.Configuration.ProcessPartitionsIndependently);
        }

        await next(context).ConfigureAwait(false);
    }
}
