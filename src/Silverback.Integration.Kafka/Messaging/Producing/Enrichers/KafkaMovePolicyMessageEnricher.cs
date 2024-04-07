// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Producing.Enrichers;

// TODO: Review double interface thing
internal sealed class KafkaMovePolicyMessageEnricher : IMovePolicyMessageEnricher<KafkaProducerEndpoint>, IMovePolicyMessageEnricher<KafkaConsumerEndpoint>
{
    public void Enrich(IRawInboundEnvelope inboundEnvelope, IOutboundEnvelope outboundEnvelope, Exception exception)
    {
        KafkaConsumer consumer = (KafkaConsumer)inboundEnvelope.Consumer;
        KafkaConsumerEndpoint consumerEndpoint = (KafkaConsumerEndpoint)inboundEnvelope.Endpoint;
        KafkaOffset offset = (KafkaOffset)inboundEnvelope.BrokerMessageIdentifier;

        outboundEnvelope.Headers.AddOrReplace(KafkaMessageHeaders.SourceTopic, consumerEndpoint.TopicPartition.Topic);
        outboundEnvelope.Headers.AddOrReplace(KafkaMessageHeaders.SourceConsumerGroupId, consumer.Configuration.GroupId);

        while (exception.InnerException != null)
        {
            exception = exception.InnerException;
        }

        outboundEnvelope.Headers.AddOrReplace(DefaultMessageHeaders.FailureReason, $"{exception.GetType().FullName} in {exception.Source}");
        outboundEnvelope.Headers.AddOrReplace(KafkaMessageHeaders.SourcePartition, consumerEndpoint.TopicPartition.Partition.Value);
        outboundEnvelope.Headers.AddOrReplace(KafkaMessageHeaders.SourceOffset, offset.Offset);

        if (inboundEnvelope.Headers.TryGetValue(KafkaMessageHeaders.Timestamp, out string? timestamp))
            outboundEnvelope.Headers.AddOrReplace(KafkaMessageHeaders.SourceTimestamp, timestamp);
    }
}
