// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Producing;

/// <summary>
///     Serializes the kafka key.
/// </summary>
// TODO: Test
public class KafkaKeySerializerProducerBehavior : IProducerBehavior
{
    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Producer.Serializer;

    /// <inheritdoc cref="IProducerBehavior.HandleAsync" />
    public ValueTask HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next, CancellationToken cancellationToken)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        if (context.Envelope is IKafkaOutboundEnvelope { Key: not null } kafkaEnvelope &&
            context.Producer.EndpointConfiguration is KafkaProducerEndpointConfiguration kafkaEndpointConfiguration)
        {
            kafkaEnvelope.SetRawKey(kafkaEndpointConfiguration.KeySerializer.Serialize(kafkaEnvelope.Key));
        }

        return next(context, cancellationToken);
    }
}
