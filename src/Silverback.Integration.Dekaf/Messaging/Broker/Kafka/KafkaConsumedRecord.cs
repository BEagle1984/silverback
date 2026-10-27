// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     A record consumed from Kafka, fully copied out of the client's fetch buffers.
/// </summary>
/// <remarks>
///     Dekaf's <c>ConsumeResult</c> borrows pooled fetch memory that is recycled once the consumer moves on, while
///     Silverback processes the records asynchronously after queueing them into the partition channels. Copying the
///     key, value and headers up front decouples the two lifetimes.
/// </remarks>
internal sealed class KafkaConsumedRecord
{
    public KafkaConsumedRecord(
        TopicPartitionOffset topicPartitionOffset,
        byte[]? key,
        byte[]? value,
        IReadOnlyCollection<MessageHeader> headers,
        DateTimeOffset timestamp)
    {
        TopicPartitionOffset = topicPartitionOffset;
        Key = key;
        Value = value;
        Headers = headers;
        Timestamp = timestamp;
    }

    private KafkaConsumedRecord(TopicPartitionOffset topicPartitionOffset)
    {
        TopicPartitionOffset = topicPartitionOffset;
        Headers = [];
        IsPartitionEof = true;
    }

    public TopicPartitionOffset TopicPartitionOffset { get; }

    public TopicPartition TopicPartition => TopicPartitionOffset.TopicPartition;

    public string Topic => TopicPartitionOffset.Topic;

    public Partition Partition => TopicPartitionOffset.Partition;

    public Offset Offset => TopicPartitionOffset.Offset;

    public byte[]? Key { get; }

    public byte[]? Value { get; }

    public IReadOnlyCollection<MessageHeader> Headers { get; }

    public DateTimeOffset Timestamp { get; }

    public bool IsPartitionEof { get; }

    public static KafkaConsumedRecord PartitionEof(TopicPartitionOffset topicPartitionOffset) => new(topicPartitionOffset);
}
