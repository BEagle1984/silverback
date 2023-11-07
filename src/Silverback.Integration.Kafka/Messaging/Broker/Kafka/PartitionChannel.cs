// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;
using Silverback.Diagnostics;

namespace Silverback.Messaging.Broker.Kafka;

internal sealed class PartitionChannel : ConsumerChannel<ConsumeResult<byte[]?, byte[]?>>
{
    public PartitionChannel(int capacity, TopicPartition topicPartition, ISilverbackLogger logger)
        : base(capacity, $"{topicPartition.Topic}[{topicPartition.Partition.Value}]", logger)
    {
        TopicPartition = topicPartition;
    }

    public TopicPartition TopicPartition { get; }
}
