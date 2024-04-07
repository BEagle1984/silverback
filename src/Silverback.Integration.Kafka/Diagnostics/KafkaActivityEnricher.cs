// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
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
    public void EnrichOutboundActivity(Activity activity, ProducerPipelineContext producerContext)
    {
        Check.NotNull(activity, nameof(activity));
        Check.NotNull(producerContext, nameof(producerContext));

        if (producerContext.Envelope.BrokerMessageIdentifier != null)
            SetMessageId(activity, (KafkaOffset)producerContext.Envelope.BrokerMessageIdentifier);

        SetMessageKey(activity, producerContext.Envelope.Headers);
    }

    /// <inheritdoc cref="IBrokerActivityEnricher.EnrichInboundActivity" />
    public void EnrichInboundActivity(Activity activity, ConsumerPipelineContext consumerContext)
    {
        Check.NotNull(activity, nameof(activity));
        Check.NotNull(consumerContext, nameof(consumerContext));

        SetMessageId(activity, (KafkaOffset)consumerContext.Envelope.BrokerMessageIdentifier);
        SetMessageKey(activity, consumerContext.Envelope.Headers);
    }

    private static void SetMessageId(Activity activity, KafkaOffset? offset)
    {
        if (offset == null)
            return;

        activity.SetTag(ActivityTagNames.MessageId, offset.ToVerboseLogString());
        activity.SetTag(KafkaPartition, $"{offset.TopicPartition.Topic}[{offset.TopicPartition.Partition.Value}]");
    }

    private static void SetMessageKey(Activity activity, MessageHeaderCollection messageHeaderCollection)
    {
        string? kafkaKeyHeaderValue = messageHeaderCollection.GetValue(DefaultMessageHeaders.MessageId);

        if (kafkaKeyHeaderValue != null)
            activity.SetTag(KafkaMessageKey, kafkaKeyHeaderValue);
    }
}
