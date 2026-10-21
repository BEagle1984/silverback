// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Diagnostics;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     The channel buffering the records consumed from a single partition, until they are processed.
/// </summary>
internal sealed class PartitionChannel : ConsumerChannel<KafkaConsumedRecord>
{
    public PartitionChannel(int capacity, TopicPartition topicPartition, ISilverbackLogger logger)
        : base(capacity, $"{topicPartition.Topic}[{topicPartition.Partition.Value}]", logger)
    {
        TopicPartition = topicPartition;
    }

    public TopicPartition TopicPartition { get; }
}
