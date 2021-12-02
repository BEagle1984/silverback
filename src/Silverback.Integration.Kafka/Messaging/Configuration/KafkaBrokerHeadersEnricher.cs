// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.Enrichers;

namespace Silverback.Messaging.Configuration
{
    internal sealed class KafkaBrokerHeadersEnricher
        : IMovePolicyMessageEnricher<KafkaProducerEndpoint>, IMovePolicyMessageEnricher<KafkaConsumerEndpoint>
    {
        public void Enrich(
            IRawInboundEnvelope inboundEnvelope,
            IOutboundEnvelope outboundEnvelope,
            Exception exception)
        {
            outboundEnvelope.Headers.AddOrReplace(
                KafkaMessageHeaders.SourceTopic,
                inboundEnvelope.ActualEndpointName);
            outboundEnvelope.Headers.AddOrReplace(
                KafkaMessageHeaders.SourceConsumerGroupId,
                ((KafkaConsumerEndpoint)inboundEnvelope.Endpoint).Configuration.GroupId);
            outboundEnvelope.Headers.AddOrReplace(
                DefaultMessageHeaders.FailureReason,
                $"{exception.GetType().FullName} in {exception.Source}");
            outboundEnvelope.Headers.AddOrReplace(
                KafkaMessageHeaders.SourcePartition,
                ((KafkaOffset)inboundEnvelope.BrokerMessageIdentifier).Partition);
            outboundEnvelope.Headers.AddOrReplace(
                KafkaMessageHeaders.SourceOffset,
                ((KafkaOffset)inboundEnvelope.BrokerMessageIdentifier).Offset);

            if (inboundEnvelope.Headers.Contains(KafkaMessageHeaders.TimestampKey))
            {
                outboundEnvelope.Headers.AddOrReplace(
                    KafkaMessageHeaders.SourceTimestamp,
                    inboundEnvelope.Headers[KafkaMessageHeaders.TimestampKey]);
            }
        }
    }
}
