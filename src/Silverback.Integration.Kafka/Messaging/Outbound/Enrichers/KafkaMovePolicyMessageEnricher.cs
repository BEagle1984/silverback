// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Outbound.Enrichers;

internal sealed class KafkaMovePolicyMessageEnricher
    : IMovePolicyMessageEnricher<KafkaProducerEndpoint>, IMovePolicyMessageEnricher<KafkaConsumerEndpoint>
{
    public void Enrich(IRawInboundEnvelope inboundEnvelope, IOutboundEnvelope outboundEnvelope, Exception exception)
    {
        KafkaConsumerEndpoint consumerEndpoint = (KafkaConsumerEndpoint)inboundEnvelope.Endpoint;
        KafkaOffset offset = (KafkaOffset)inboundEnvelope.BrokerMessageIdentifier;

        outboundEnvelope.Headers.AddOrReplace(KafkaMessageHeaders.SourceTopic, consumerEndpoint.TopicPartition.Topic);
        outboundEnvelope.Headers.AddOrReplace(KafkaMessageHeaders.SourceConsumerGroupId, consumerEndpoint.Configuration.Client.GroupId);

        while (exception.InnerException != null)
        {
            exception = exception.InnerException;
        }

        outboundEnvelope.Headers.AddOrReplace(DefaultMessageHeaders.FailureReason, $"{exception.GetType().FullName} in {exception.Source}");
        outboundEnvelope.Headers.AddOrReplace(KafkaMessageHeaders.SourcePartition, consumerEndpoint.TopicPartition.Partition.Value);
        outboundEnvelope.Headers.AddOrReplace(KafkaMessageHeaders.SourceOffset, offset.Offset);

        if (inboundEnvelope.Headers.TryGetValue(KafkaMessageHeaders.TimestampKey, out string? timestamp))
            outboundEnvelope.Headers.AddOrReplace(KafkaMessageHeaders.SourceTimestamp, timestamp);
    }
}
