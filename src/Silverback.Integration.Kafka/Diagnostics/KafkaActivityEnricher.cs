// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Diagnostics;

/// <summary>
///     Enriches the <see cref="Activity" /> with Kafka specific tags.
/// </summary>
public class KafkaActivityEnricher : IBrokerActivityEnricher<KafkaConsumerEndpointConfiguration>, IBrokerActivityEnricher<KafkaProducerEndpointConfiguration>
{
    internal const string KafkaMessageKey = "messaging.kafka.message_key";

    internal const string KafkaPartition = "messaging.kafka.partition";

    /// <inheritdoc cref="IBrokerActivityEnricher.EnrichOutboundActivity" />
    public void EnrichOutboundActivity(Activity activity, IOutboundEnvelope envelope)
    {
        Check.NotNull(activity);
        Check.NotNull(envelope);

        Enrich(activity, (KafkaOffset?)envelope.BrokerMessageIdentifier, envelope.Headers);
    }

    /// <inheritdoc cref="IBrokerActivityEnricher.EnrichInboundActivity" />
    public void EnrichInboundActivity(Activity activity, IRawInboundEnvelope envelope)
    {
        Check.NotNull(activity);
        Check.NotNull(envelope);

        Enrich(activity, (KafkaOffset)envelope.BrokerMessageIdentifier, envelope.Headers);
    }

    private static void Enrich(Activity activity, KafkaOffset? offset, MessageHeaderCollection headers)
    {
        if (offset != null)
        {
            activity.SetTag(ActivityTagNames.MessageId, offset.ToVerboseLogString());
            activity.SetTag(KafkaPartition, $"{offset.TopicPartition.Topic}[{offset.TopicPartition.Partition.Value}]");
        }

        string? kafkaKey = headers.GetValue(KafkaMessageHeaders.MessageKey);

        if (kafkaKey != null)
            activity.SetTag(KafkaMessageKey, kafkaKey);
    }
}
