// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;

namespace Silverback;

internal static class TopicPartitionExtensions
{
    public static string ToDisplayString(this TopicPartition topicPartition) =>
        topicPartition.Partition == Partition.Any
            ? topicPartition.Topic
            : $"{topicPartition.Topic}[{topicPartition.Partition.Value}]";
}
