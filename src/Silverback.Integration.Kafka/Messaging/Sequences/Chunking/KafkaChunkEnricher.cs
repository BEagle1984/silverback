// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Sequences.Chunking;

/// <summary>
///     Enriches the produced chunks adding some additional Kafka-specific headers.
/// </summary>
public class KafkaChunkEnricher : IChunkEnricher
{
    /// <inheritdoc cref="IChunkEnricher.GetFirstChunkMessageHeader" />
    public MessageHeader? GetFirstChunkMessageHeader(IOutboundEnvelope envelope) =>
        Check.NotNull(envelope, nameof(envelope)).BrokerMessageIdentifier is KafkaOffset offset
            ? new MessageHeader(KafkaMessageHeaders.FirstChunkOffset, offset.Offset.Value)
            : null;
}
