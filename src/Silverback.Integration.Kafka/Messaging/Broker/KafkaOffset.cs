// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Confluent.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

/// <summary>
///     Represents the position of the message in a partition.
/// </summary>
public sealed record KafkaOffset : IBrokerMessageIdentifier, IComparable<KafkaOffset>, IComparable
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaOffset" /> class.
    /// </summary>
    /// <param name="topicPartitionOffset">
    ///     The <see cref="Confluent.Kafka.TopicPartitionOffset" />.
    /// </param>
    public KafkaOffset(TopicPartitionOffset topicPartitionOffset)
        : this(
            Check.NotNull(topicPartitionOffset, nameof(topicPartitionOffset)).TopicPartition,
            Check.NotNull(topicPartitionOffset, nameof(topicPartitionOffset)).Offset.Value)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaOffset" /> class.
    /// </summary>
    /// <param name="topicPartition">
    ///     The <see cref="Confluent.Kafka.TopicPartition" />.
    /// </param>
    /// <param name="offset">
    ///     The offset in the partition.
    /// </param>
    public KafkaOffset(TopicPartition topicPartition, Offset offset)
    {
        TopicPartition = Check.NotNull(topicPartition, nameof(topicPartition));
        Offset = Check.NotNull(offset, nameof(offset));
    }

    /// <summary>
    ///     Gets the topic and partition.
    /// </summary>
    public TopicPartition TopicPartition { get; }

    /// <summary>
    ///     Gets the offset in the partition.
    /// </summary>
    public Offset Offset { get; }

    /// <summary>
    ///     Less than operator.
    /// </summary>
    /// <param name="left">
    ///     Left-hand operand.
    /// </param>
    /// <param name="right">
    ///     Right-hand operand.
    /// </param>
    public static bool operator <(KafkaOffset left, KafkaOffset right) =>
        Comparer<KafkaOffset>.Default.Compare(left, right) < 0;

    /// <summary>
    ///     Greater than operator.
    /// </summary>
    /// <param name="left">
    ///     Left-hand operand.
    /// </param>
    /// <param name="right">
    ///     Right-hand operand.
    /// </param>
    public static bool operator >(KafkaOffset left, KafkaOffset right) =>
        Comparer<KafkaOffset>.Default.Compare(left, right) > 0;

    /// <summary>
    ///     Less than or equal operator.
    /// </summary>
    /// <param name="left">
    ///     Left-hand operand.
    /// </param>
    /// <param name="right">
    ///     Right-hand operand.
    /// </param>
    public static bool operator <=(KafkaOffset left, KafkaOffset right) =>
        Comparer<KafkaOffset>.Default.Compare(left, right) <= 0;

    /// <summary>
    ///     Greater than or equal operator.
    /// </summary>
    /// <param name="left">
    ///     Left-hand operand.
    /// </param>
    /// <param name="right">
    ///     Right-hand operand.
    /// </param>
    public static bool operator >=(KafkaOffset left, KafkaOffset right) =>
        Comparer<KafkaOffset>.Default.Compare(left, right) >= 0;

    /// <inheritdoc cref="IBrokerMessageIdentifier.ToLogString" />
    public string ToLogString() => $"[{TopicPartition.Partition.Value}]@{Offset}";

    /// <inheritdoc cref="IBrokerMessageIdentifier.ToVerboseLogString" />
    public string ToVerboseLogString() => $"{TopicPartition.Topic}[{TopicPartition.Partition.Value}]@{Offset}";

    /// <inheritdoc cref="IComparable{T}.CompareTo" />
    public int CompareTo(KafkaOffset? other)
    {
        if (ReferenceEquals(this, other))
            return 0;

        if (other is null)
            return 1;

        return Offset.Value.CompareTo(other.Offset.Value);
    }

    /// <inheritdoc cref="IComparable.CompareTo" />
    public int CompareTo(object? obj) => obj is KafkaOffset otherOffset ? CompareTo(otherOffset) : -1;

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(IBrokerMessageIdentifier? other) => other is KafkaOffset kafkaOffset && Equals(kafkaOffset);

    internal TopicPartitionOffset AsTopicPartitionOffset() => new(TopicPartition, Offset);
}
