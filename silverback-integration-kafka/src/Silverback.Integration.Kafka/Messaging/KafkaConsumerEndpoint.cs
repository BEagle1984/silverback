// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging
{
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public class KafkaConsumerEndpoint : KafkaEndpoint, IEquatable<KafkaConsumerEndpoint>
    {
        private const bool KafkaDefaultAutoCommitEnabled = true;

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
        /// The number of threads to be started to consumer the endpoint. The default is 1.
        /// </summary>
        public int ConsumerThreads { get; set; } = 1;

        /// <summary>
        /// Gets a boo loan value indicating whether autocommit is enabled according to the explicit configuration and
        /// Kafka defaults.
        /// </summary>
        public bool IsAutoCommitEnabled => Configuration?.EnableAutoCommit ?? KafkaDefaultAutoCommitEnabled;

        public override void Validate()
        {
            base.Validate();

            if (CommitOffsetEach < 1)
                throw new EndpointConfigurationException("CommitOffSetEach must be greater or equal to 1.");

            if (ConsumerThreads < 1)
                throw new EndpointConfigurationException("ConsumerThreads must be greater or equal to 1.");

            if (ReuseConsumer && ConsumerThreads > 1)
                throw new EndpointConfigurationException(
                    "It is not allowed to set ReuseConsumer = true with multiple threads.");

            if (!IsAutoCommitEnabled && CommitOffsetEach % Batch.Size != 0)
                throw new EndpointConfigurationException(
                    "CommitOffsetEach must be a multiple of BatchSize if EnableAutoCommit is false.");

            if (Configuration.EnableAutoOffsetStore == true)
                throw new EndpointConfigurationException("EnableAutoOffsetStore is not supported. Silverback must have control of the offset storing to work properly.");

            Configuration.EnableAutoOffsetStore = false;
        }

        #region IEquatable

        #endregion

        public bool Equals(KafkaConsumerEndpoint other)
        {
            return base.Equals(other) &&
                   Equals(Configuration, other.Configuration) && 
                   CommitOffsetEach == other.CommitOffsetEach && 
                   ReuseConsumer == other.ReuseConsumer && 
                   ConsumerThreads == other.ConsumerThreads;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj) &&
                   obj is KafkaConsumerEndpoint endpoint && Equals(endpoint);
        }
    }
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
}