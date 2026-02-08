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
///     Serializes the MQTT correlation data.
/// </summary>
// TODO: Test
public class MqttCorrelationDataSerializerProducerBehavior : IProducerBehavior
{
    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Producer.Serializer;

    /// <inheritdoc cref="IProducerBehavior.HandleAsync" />
    public ValueTask HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next, CancellationToken cancellationToken)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        // TODO: Support other data types via IKafkaKeySerializer
        if (context.Envelope is IMqttOutboundEnvelope<object, byte[]> { CorrelationData: not null } mqttEnvelope)
            mqttEnvelope.SetMqttRawCorrelationData(mqttEnvelope.CorrelationData);

        return next(context, cancellationToken);
    }
}
