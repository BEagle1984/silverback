// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Silverback.Messaging.Broker
{
    public sealed class KafkaOffset : IOffset, IComparable<KafkaOffset>
    {
        [JsonConstructor]
        public KafkaOffset(string topic, int partition, long offset)
        {
            Topic = topic;
            Partition = partition;
            Offset = offset;

            Key = $"{topic}[{partition}]";
            Value = $"{partition}@{offset}";
        }

        public KafkaOffset(Confluent.Kafka.TopicPartitionOffset topicPartitionOffset)
            : this(topicPartitionOffset.Topic, topicPartitionOffset.Partition.Value, topicPartitionOffset.Offset.Value)
        {
        }

        /// <summary>
        /// Gets the name of the topic.
        /// </summary>
        public string Topic { get; }
        
        /// <summary>
        /// Gets the partition number.
        /// </summary>
        public int Partition { get; }
        
        /// <summary>
        /// Gets the offset in the partition.
        /// </summary>
        public long Offset { get; }
        
        /// <inheritdoc cref="IOffset"/>
        public string Key { get; }

        /// <inheritdoc cref="IOffset"/>
        public string Value { get; }

        public int CompareTo(KafkaOffset other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (other is null) return 1;
            return Offset.CompareTo(other.Offset);
        }

        public int CompareTo(IOffset obj)
        {
            if (ReferenceEquals(this, obj)) return 0;
            if (obj is null) return 1;
            return obj is KafkaOffset other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(KafkaOffset)}");
        }

        public static bool operator <(KafkaOffset left, KafkaOffset right) => Comparer<KafkaOffset>.Default.Compare(left, right) < 0;

        public static bool operator >(KafkaOffset left, KafkaOffset right) => Comparer<KafkaOffset>.Default.Compare(left, right) > 0;

        public static bool operator <=(KafkaOffset left, KafkaOffset right) => Comparer<KafkaOffset>.Default.Compare(left, right) <= 0;

        public static bool operator >=(KafkaOffset left, KafkaOffset right) => Comparer<KafkaOffset>.Default.Compare(left, right) >= 0;

        internal Confluent.Kafka.TopicPartitionOffset AsTopicPartitionOffset() =>
            new Confluent.Kafka.TopicPartitionOffset(Topic, new Confluent.Kafka.Partition(Partition),
                new Confluent.Kafka.Offset(Offset));
    }
}
