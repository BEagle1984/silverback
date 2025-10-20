// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Producing;

/// <summary>
///     Serializes the kafka key.
/// </summary>
public class KafkaKeySerializerProducerBehavior : IProducerBehavior
{
    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Producer.Serializer;

    /// <inheritdoc cref="IProducerBehavior.HandleAsync" />
    public ValueTask HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next, CancellationToken cancellationToken)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        // TODO: Support other key types via IKafkaKeySerializer
        if (context.Envelope is IKafkaOutboundEnvelope<object, string> { Key: not null } kafkaEnvelope)
            kafkaEnvelope.SetRawKey(Encoding.UTF8.GetBytes(kafkaEnvelope.Key));

        return next(context, cancellationToken);
    }
}
