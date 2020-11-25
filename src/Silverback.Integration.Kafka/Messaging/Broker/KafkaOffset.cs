// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Globalization;
using Confluent.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="IComparableOffset" />
    public sealed class KafkaOffset : IComparableOffset
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaOffset" /> class.
        /// </summary>
        /// <param name="key">
        ///     The unique key of the queue, topic or partition this offset belongs to.
        /// </param>
        /// <param name="value">
        ///     The offset value.
        /// </param>
        public KafkaOffset(string key, string value)
        {
            Check.NotEmpty(key, nameof(key));
            Check.NotEmpty(value, nameof(value));

            Key = key;
            Value = value;

            int topicPartitionSeparatorIndex = key.IndexOf('[', StringComparison.Ordinal);
            Topic = key.Substring(0, topicPartitionSeparatorIndex);
            Partition = int.Parse(
                key.Substring(topicPartitionSeparatorIndex + 1, key.Length - topicPartitionSeparatorIndex - 2),
                CultureInfo.InvariantCulture);
            Offset = int.Parse(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaOffset" /> class.
        /// </summary>
        /// <param name="topic">
        ///     The name of the topic.
        /// </param>
        /// <param name="partition">
        ///     The partition number.
        /// </param>
        /// <param name="offset">
        ///     The offset in the partition.
        /// </param>
        public KafkaOffset(string topic, int partition, long offset)
        {
            Check.NotEmpty(topic, nameof(topic));

            Topic = topic;
            Partition = partition;
            Offset = offset;

            Key = $"{topic}[{partition}]";
            Value = offset.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaOffset" /> class.
        /// </summary>
        /// <param name="topicPartitionOffset">
        ///     The <see cref="Confluent.Kafka.TopicPartitionOffset" />.
        /// </param>
        public KafkaOffset(TopicPartitionOffset topicPartitionOffset)
            : this(
                Check.NotNull(topicPartitionOffset, nameof(topicPartitionOffset)).Topic,
                Check.NotNull(topicPartitionOffset, nameof(topicPartitionOffset)).Partition.Value,
                Check.NotNull(topicPartitionOffset, nameof(topicPartitionOffset)).Offset.Value)
        {
        }

        /// <summary>
        ///     Gets the name of the topic.
        /// </summary>
        public string Topic { get; }

        /// <summary>
        ///     Gets the partition number.
        /// </summary>
        public int Partition { get; }

        /// <summary>
        ///     Gets the offset in the partition.
        /// </summary>
        public long Offset { get; }

        /// <inheritdoc cref="IOffset.Key" />
        public string Key { get; }

        /// <inheritdoc cref="IOffset.Value" />
        public string Value { get; }

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

        /// <summary>
        ///     Equality operator.
        /// </summary>
        /// <param name="left">
        ///     Left-hand operand.
        /// </param>
        /// <param name="right">
        ///     Right-hand operand.
        /// </param>
        public static bool operator ==(KafkaOffset? left, KafkaOffset? right)
        {
            if (ReferenceEquals(left, null))
                return ReferenceEquals(right, null);

            return left.Equals(right);
        }

        /// <summary>
        ///     Inequality operator.
        /// </summary>
        /// <param name="left">
        ///     Left-hand operand.
        /// </param>
        /// <param name="right">
        ///     Right-hand operand.
        /// </param>
        public static bool operator !=(KafkaOffset left, KafkaOffset right) => !(left == right);

        /// <summary>
        ///     Compares the current instance with another object of the same type and returns an integer that
        ///     indicates whether the current instance precedes, follows, or occurs in the same position in the sort
        ///     order as the other object.
        /// </summary>
        /// <param name="other">
        ///     An object to compare with the current instance.
        /// </param>
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

        /// <inheritdoc cref="IComparable{T}.CompareTo" />
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

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(IOffset? other)
        {
            if (ReferenceEquals(this, other))
                return true;
            if (ReferenceEquals(other, null))
                return false;

            if (!(other is KafkaOffset otherKafkaOffset))
                return false;

            return Topic == otherKafkaOffset.Topic &&
                   Partition == otherKafkaOffset.Partition &&
                   Offset == otherKafkaOffset.Offset;
        }

        /// <inheritdoc cref="object.Equals(object)" />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != GetType())
                return false;

            return Equals((IOffset)obj);
        }

        /// <inheritdoc cref="object.GetHashCode" />
        public override int GetHashCode() => HashCode.Combine(Topic, Partition, Offset);

        internal TopicPartitionOffset AsTopicPartitionOffset() =>
            new TopicPartitionOffset(
                Topic,
                new Partition(Partition),
                new Offset(Offset));
    }
}
