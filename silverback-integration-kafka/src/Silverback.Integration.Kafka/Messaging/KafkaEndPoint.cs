// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging
{
    public class KafkaConsumerEndpoint : KafkaEndpoint, IEquatable<KafkaConsumerEndpoint>
    {
        public KafkaConsumerEndpoint(string name) : base(name)
        {
        }

        /// <summary>
        /// Define the number of message processed before committing the offset to the server.
        /// The most reliable level is 1 but it reduces throughput.
        /// </summary>
        public int CommitOffsetEach { get; set; } = 1;

        /// <summary>
        /// The maximum time (in milliseconds, -1 to block indefinitely) within which the poll remain blocked.
        /// </summary>
        public int PollTimeout { get; set; } = 100;

        /// <summary>
        /// If set to <c>true</c> it will resuse an instance of <see cref="Confluent.Kafka.Consumer"/> with 
        /// the same settings when possible, actually using the same consumer to subscribe to multiple topics.
        /// Default is <c>false</c>.
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
            return string.Equals(Name, other.Name) && Equals(Serializer, other.Serializer) && Equals(Configuration, other.Configuration) && CommitOffsetEach == other.CommitOffsetEach && PollTimeout == other.PollTimeout;
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

        public abstract class KafkaEndpoint : IEndpoint
    {
        protected KafkaEndpoint(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets the topic name.
        /// </summary>
        public string Name { get; }

        public IMessageSerializer Serializer { get; set; } = new JsonMessageSerializer();

        public KafkaConfigurationDictionary Configuration { get; set; }
    }
}
