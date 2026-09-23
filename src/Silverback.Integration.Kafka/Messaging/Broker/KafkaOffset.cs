// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Confluent.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

/// <summary>
///     Represents the position of the message in a partition.
/// </summary>
/// <remarks>
///     Equality and hashing depend only on the topic, partition and offset. The channel that delivered the message is ignored.
/// </remarks>
public sealed record KafkaOffset : IBrokerMessageIdentifier, IComparable<KafkaOffset>, IComparable
{
    private readonly Guid? _sourceChannelInstanceId;

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
    /// <param name="topic">
    ///     The topic.
    /// </param>
    /// <param name="partition">
    ///     The partition.
    /// </param>
    /// <param name="offset">
    ///     The offset in the partition.
    /// </param>
    public KafkaOffset(string topic, int partition, long offset)
        : this(new TopicPartition(Check.NotNull(topic, nameof(topic)), partition), offset)
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

    internal KafkaOffset(TopicPartitionOffset topicPartitionOffset, Guid sourceChannelInstanceId)
        : this(topicPartitionOffset)
    {
        _sourceChannelInstanceId = sourceChannelInstanceId;
    }

    private KafkaOffset(TopicPartition topicPartition, Offset offset, Guid? sourceChannelInstanceId)
        : this(topicPartition, offset)
    {
        _sourceChannelInstanceId = sourceChannelInstanceId;
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
    ///     Gets a value indicating whether the offset is one of the special values (i.e. <see cref="Offset.Unset" />,
    ///     <see cref="Offset.Beginning" />, <see cref="Offset.End" />).
    /// </summary>
    public bool IsSpecial => Offset.IsSpecial;

    /// <summary>
    ///     Gets a value indicating whether the offset has a source channel.
    /// </summary>
    internal bool HasSourceChannel => _sourceChannelInstanceId.HasValue;

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

    /// <inheritdoc />
    public bool Equals(KafkaOffset? other) => other != null && TopicPartition.Equals(other.TopicPartition) && Offset.Equals(other.Offset);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(TopicPartition, Offset);

    /// <summary>
    ///     Determines whether this offset originated from the specified channel instance, independently of position equality.
    /// </summary>
    internal bool BelongsToChannel(Guid channelInstanceId) => _sourceChannelInstanceId == channelInstanceId;

    // Keep the originating channel when a tracker advances the position after committing a message
    internal KafkaOffset GetNextOffset() => new(TopicPartition, Offset + 1, _sourceChannelInstanceId);

    internal TopicPartitionOffset AsTopicPartitionOffset() => new(TopicPartition, Offset);
}
