// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Outbound.Routing
{
    /// <summary>
    ///     Resolves the target partition for the message being published using the
    ///     <see cref="KafkaProducerEndpoint.GetPartition" /> method.
    /// </summary>
    public class KafkaPartitionResolverProducerBehavior : IProducerBehavior
    {
        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Producer.EndpointNameResolver + 1;

        /// <inheritdoc cref="IProducerBehavior.HandleAsync" />
        public async Task HandleAsync(
            ProducerPipelineContext context,
            ProducerBehaviorHandler next,
            CancellationToken cancellationToken = default)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            if (context.Envelope.Endpoint is KafkaProducerEndpoint kafkaProducerEndpoint)
            {
                var partition = kafkaProducerEndpoint.GetPartition(context.Envelope, context.ServiceProvider);

                if (partition != Partition.Any)
                {
                    context.Envelope.Headers.AddOrReplace(
                        KafkaMessageHeaders.KafkaPartitionIndex,
                        partition.Value);
                }
            }

            await next(context, cancellationToken).ConfigureAwait(false);
        }
    }
}
