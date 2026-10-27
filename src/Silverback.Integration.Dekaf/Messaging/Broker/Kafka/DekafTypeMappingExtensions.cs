// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Dekaf.Producer;
using DekafTopicPartition = Dekaf.TopicPartition;
using DekafTopicPartitionOffset = Dekaf.TopicPartitionOffset;
using DekafTopicPartitionTimestamp = Dekaf.TopicPartitionTimestamp;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     Maps the topic, partition and offset types between Silverback and Dekaf.
/// </summary>
internal static class DekafTypeMappingExtensions
{
    public static DekafTopicPartition ToDekaf(this TopicPartition topicPartition) =>
        new(topicPartition.Topic, topicPartition.Partition);

    public static DekafTopicPartitionOffset ToDekaf(this TopicPartitionOffset topicPartitionOffset) =>
        new(topicPartitionOffset.Topic, topicPartitionOffset.Partition, topicPartitionOffset.Offset);

    public static DekafTopicPartitionTimestamp ToDekaf(this TopicPartitionTimestamp topicPartitionTimestamp) =>
        new(
            topicPartitionTimestamp.TopicPartition.Topic,
            topicPartitionTimestamp.TopicPartition.Partition,
            topicPartitionTimestamp.Timestamp.ToUnixTimeMilliseconds());

    public static TopicPartition ToSilverback(this DekafTopicPartition topicPartition) =>
        new(topicPartition.Topic, topicPartition.Partition);

    public static TopicPartitionOffset ToSilverback(this DekafTopicPartition topicPartition, Offset offset) =>
        new(topicPartition.Topic, topicPartition.Partition, offset);

    public static TopicPartitionOffset ToTopicPartitionOffset(this RecordMetadata metadata) =>
        new(metadata.Topic, metadata.Partition, metadata.Offset);
}
