// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Confluent.Kafka;
using Newtonsoft.Json;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="IComparableOffset" />
    public sealed class KafkaOffset : IComparableOffset
    {
        /// <summary> Initializes a new instance of the <see cref="KafkaOffset" /> class. </summary>
        /// <param name="topic"> The name of the topic. </param>
        /// <param name="partition"> The partition number. </param>
        /// <param name="offset"> The offset in the partition. </param>
        [JsonConstructor]
        public KafkaOffset(string topic, int partition, long offset)
        {
            Topic = topic;
            Partition = partition;
            Offset = offset;

            Key = $"{topic}[{partition}]";
            Value = $"{offset}";
        }

        /// <summary> Initializes a new instance of the <see cref="KafkaOffset" /> class. </summary>
        /// <param name="topicPartitionOffset"> The <see cref="Confluent.Kafka.TopicPartitionOffset" />. </param>
        public KafkaOffset(TopicPartitionOffset topicPartitionOffset)
            : this(
                Check.NotNull(topicPartitionOffset, nameof(topicPartitionOffset)).Topic,
                Check.NotNull(topicPartitionOffset, nameof(topicPartitionOffset)).Partition.Value,
                Check.NotNull(topicPartitionOffset, nameof(topicPartitionOffset)).Offset.Value)
        {
        }

        /// <summary> Gets the name of the topic. </summary>
        public string Topic { get; }

        /// <summary> Gets the partition number. </summary>
        public int Partition { get; }

        /// <summary> Gets the offset in the partition. </summary>
        public long Offset { get; }

        /// <inheritdoc />
        public string Key { get; }

        /// <inheritdoc />
        public string Value { get; }

        /// <summary> Less than operator. </summary>
        /// <param name="left"> Left-hand operand. </param>
        /// <param name="right"> Right-hand operand. </param>
        public static bool operator <(KafkaOffset left, KafkaOffset right) =>
            Comparer<KafkaOffset>.Default.Compare(left, right) < 0;

        /// <summary> Greater than operator. </summary>
        /// <param name="left"> Left-hand operand. </param>
        /// <param name="right"> Right-hand operand. </param>
        public static bool operator >(KafkaOffset left, KafkaOffset right) =>
            Comparer<KafkaOffset>.Default.Compare(left, right) > 0;

        /// <summary> Less than or equal operator. </summary>
        /// <param name="left"> Left-hand operand. </param>
        /// <param name="right"> Right-hand operand. </param>
        public static bool operator <=(KafkaOffset left, KafkaOffset right) =>
            Comparer<KafkaOffset>.Default.Compare(left, right) <= 0;

        /// <summary> Greater than or equal operator. </summary>
        /// <param name="left"> Left-hand operand. </param>
        /// <param name="right"> Right-hand operand. </param>
        public static bool operator >=(KafkaOffset left, KafkaOffset right) =>
            Comparer<KafkaOffset>.Default.Compare(left, right) >= 0;

        /// <summary> Equality operator. </summary>
        /// <param name="left"> Left-hand operand. </param>
        /// <param name="right"> Right-hand operand. </param>
        public static bool operator ==(KafkaOffset left, KafkaOffset right)
        {
            if (ReferenceEquals(left, null))
                return ReferenceEquals(right, null);

            return left.Equals(right);
        }

        /// <summary> Inequality operator. </summary>
        /// <param name="left"> Left-hand operand. </param>
        /// <param name="right"> Right-hand operand. </param>
        public static bool operator !=(KafkaOffset left, KafkaOffset right) => !(left == right);

        /// <inheritdoc />
        public string ToLogString() => $"{Partition}@{Offset}";

        /// <summary>
        ///     Compares the current instance with another object of the same type and returns an integer that
        ///     indicates whether the current instance precedes, follows, or occurs in the same position in the sort
        ///     order as the other object.
        /// </summary>
        /// <param name="other"> An object to compare with the current instance. </param>
        /// <returns>
        ///     A value less than zero if this is less than object, zero if this is equal to object, or a value
        ///     greater than zero if this is greater than object.
        /// </returns>
        public int CompareTo(KafkaOffset? other)
        {
            if (ReferenceEquals(this, other))
                return 0;

            if (other is null)
                return 1;

            return Offset.CompareTo(other.Offset);
        }

        /// <inheritdoc />
        public int CompareTo(IOffset? other)
        {
            if (ReferenceEquals(this, other))
                return 0;

            if (other is null)
                return 1;

            return other is KafkaOffset otherKafkaOffset
                ? CompareTo(otherKafkaOffset)
                : throw new ArgumentException($"Object must be of type {nameof(KafkaOffset)}");
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            if (ReferenceEquals(obj, null))
                return false;

            if (!(obj is KafkaOffset other))
                return false;

            return CompareTo(other) == 0;
        }

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Topic, Partition, Offset);

        internal TopicPartitionOffset AsTopicPartitionOffset() =>
            new TopicPartitionOffset(
                Topic,
                new Partition(Partition),
                new Offset(Offset));
    }
}
