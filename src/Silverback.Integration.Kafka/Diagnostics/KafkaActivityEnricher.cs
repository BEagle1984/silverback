// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Diagnostics;

/// <summary>
///     Enriches the <see cref="Activity" /> with Kafka specific tags.
/// </summary>
public class KafkaActivityEnricher : IBrokerActivityEnricher<KafkaConsumerConfiguration>, IBrokerActivityEnricher<KafkaProducerConfiguration>
{
    internal const string KafkaMessageKey = "messaging.kafka.message_key";

    internal const string KafkaPartition = "messaging.kafka.partition";

    /// <inheritdoc cref="IBrokerActivityEnricher.EnrichOutboundActivity" />
    public void EnrichOutboundActivity(Activity activity, ProducerPipelineContext producerContext)
    {
        Check.NotNull(activity, nameof(activity));
        Check.NotNull(producerContext, nameof(producerContext));

        SetMessageId(activity, producerContext.Envelope.BrokerMessageIdentifier);
        SetMessageKey(activity, producerContext.Envelope.Headers);
    }

    /// <inheritdoc cref="IBrokerActivityEnricher.EnrichInboundActivity" />
    public void EnrichInboundActivity(Activity activity, ConsumerPipelineContext consumerContext)
    {
        Check.NotNull(activity, nameof(activity));
        Check.NotNull(consumerContext, nameof(consumerContext));

        SetMessageId(activity, consumerContext.Envelope.BrokerMessageIdentifier);
        SetMessageKey(activity, consumerContext.Envelope.Headers);
    }

    private static void SetMessageId(Activity activity, IBrokerMessageIdentifier? messageId)
    {
        if (messageId != null)
        {
            activity.SetTag(ActivityTagNames.MessageId, messageId.ToVerboseLogString());
            activity.SetTag(KafkaPartition, messageId.Key);
        }
    }

    private static void SetMessageKey(Activity activity, MessageHeaderCollection messageHeaderCollection)
    {
        string? kafkaKeyHeaderValue =
            messageHeaderCollection.GetValue(KafkaMessageHeaders.KafkaMessageKey);
        if (kafkaKeyHeaderValue != null)
        {
            activity.SetTag(KafkaMessageKey, kafkaKeyHeaderValue);
        }
    }
}
