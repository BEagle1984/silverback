// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Kafka;

internal sealed class PartitionChannel : ConsumerChannel<ConsumeResult<byte[]?, byte[]?>>
{
    public PartitionChannel(int capacity, string id, TopicPartition topicPartition)
        : base(capacity, id)
    {
        TopicPartition = topicPartition;
    }

    public TopicPartition TopicPartition { get; }
}
