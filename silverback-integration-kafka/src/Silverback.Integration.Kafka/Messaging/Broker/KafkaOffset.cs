// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Broker
{
    public sealed class KafkaOffset : IOffset, IComparable<KafkaOffset>
    {
        public KafkaOffset(Confluent.Kafka.TopicPartitionOffset topicPartitionOffset)
        {
            TopicPartitionOffset = topicPartitionOffset ?? throw new ArgumentNullException(nameof(topicPartitionOffset));
            Key = topicPartitionOffset.TopicPartition.ToString();
            Value = topicPartitionOffset.Offset.Value.ToString();
        }

        public Confluent.Kafka.TopicPartitionOffset TopicPartitionOffset { get; }
        public string Key { get; }
        public string Value { get; }

        public int CompareTo(KafkaOffset other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return TopicPartitionOffset.Offset.Value.CompareTo(other.TopicPartitionOffset.Offset.Value);
        }

        public int CompareTo(object obj)
        {
            if (ReferenceEquals(null, obj)) return 1;
            if (ReferenceEquals(this, obj)) return 0;
            return obj is KafkaOffset other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(KafkaOffset)}");
        }

        public static bool operator <(KafkaOffset left, KafkaOffset right) => Comparer<KafkaOffset>.Default.Compare(left, right) < 0;

        public static bool operator >(KafkaOffset left, KafkaOffset right) => Comparer<KafkaOffset>.Default.Compare(left, right) > 0;

        public static bool operator <=(KafkaOffset left, KafkaOffset right) => Comparer<KafkaOffset>.Default.Compare(left, right) <= 0;

        public static bool operator >=(KafkaOffset left, KafkaOffset right) => Comparer<KafkaOffset>.Default.Compare(left, right) >= 0;
    }
}
