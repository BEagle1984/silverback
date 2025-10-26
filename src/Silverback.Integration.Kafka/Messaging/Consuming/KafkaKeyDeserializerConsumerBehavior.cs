// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Consuming;

/// <summary>
///     Deserializes the kafka key.
/// </summary>
// TODO: Test
public class KafkaKeyDeserializerConsumerBehavior : IConsumerBehavior
{
    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Deserializer;

    /// <inheritdoc cref="IConsumerBehavior.HandleAsync" />
    public ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next, CancellationToken cancellationToken)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        // TODO: Support other key types via IKafkaKeySerializer
        if (context.Envelope is IInternalKafkaInboundEnvelope { RawKey.Length: > 0, Key: null } kafkaEnvelope)
            kafkaEnvelope.SetKey(Encoding.UTF8.GetString(kafkaEnvelope.RawKey));

        return next(context, cancellationToken);
    }
}
