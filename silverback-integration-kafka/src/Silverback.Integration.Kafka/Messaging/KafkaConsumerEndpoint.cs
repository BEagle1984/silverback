// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging
{
    public class KafkaConsumerEndpoint : KafkaEndpoint, IEquatable<KafkaConsumerEndpoint>
    {
        public KafkaConsumerEndpoint(string name) : base(name)
        {
        }

        public Confluent.Kafka.ConsumerConfig Configuration { get; set; } = new Confluent.Kafka.ConsumerConfig();

        /// <summary>
        /// Defines the number of message processed before committing the offset to the server.
        /// The most reliable level is 1 but it reduces throughput.
        /// </summary>
        public int CommitOffsetEach { get; set; } = 1;

        /// <summary>
        /// If set to <c>true</c> it will reuse an instance of <see cref="Confluent.Kafka.Consumer{TKey, TValue}"/> with 
        /// the same settings when possible, actually using the same consumer to subscribe to multiple topics.
        /// The default value is <c>false</c>.
        /// </summary>
        public bool ReuseConsumer { get; set; } = false;

        /// <summary>
        /// The amount of threads to be started to consumer the endpoint. The default is 1.
        /// </summary>
        public int ConsumerThreads { get; set; } = 1;

        #region IEquatable

        public bool Equals(KafkaConsumerEndpoint other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name) && Equals(Serializer, other.Serializer) && KafkaClientConfigComparer.Compare(Configuration, other.Configuration) && CommitOffsetEach == other.CommitOffsetEach;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is KafkaConsumerEndpoint && Equals((KafkaConsumerEndpoint)obj);
        }

        public override int GetHashCode() => Name?.GetHashCode() ?? 0;

        #endregion
    }
}