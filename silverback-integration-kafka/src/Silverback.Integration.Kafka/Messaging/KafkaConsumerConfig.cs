// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging
{
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public sealed class KafkaConsumerConfig : Confluent.Kafka.ConsumerConfig, IEquatable<KafkaConsumerConfig>
    {
        private const bool KafkaDefaultAutoCommitEnabled = true;

        /// <summary>
        /// Defines the number of message processed before committing the offset to the server.
        /// The most reliable level is 1 but it reduces throughput.
        /// </summary>
        public int CommitOffsetEach { get; set; } = -1;

        /// <summary>
        /// Gets a boo loan value indicating whether autocommit is enabled according to the explicit configuration and
        /// Kafka defaults.
        /// </summary>
        public bool IsAutoCommitEnabled => EnableAutoCommit ?? KafkaDefaultAutoCommitEnabled;

        public void Validate()
        {
            if (IsAutoCommitEnabled && CommitOffsetEach > 0)
            {
                throw new EndpointConfigurationException(
                    "CommitOffsetEach cannot be used when auto-commit is enabled. " +
                    "Explicitly disable it setting Configuration.EnableAutoCommit = false.");
            }

            if (!IsAutoCommitEnabled && CommitOffsetEach <= 0)
            {
                throw new EndpointConfigurationException(
                    "CommitOffSetEach must be greater or equal to 1 when auto-commit is disabled.");
            }

            if (EnableAutoOffsetStore == true)
            {
                throw new EndpointConfigurationException(
                    "EnableAutoOffsetStore is not supported. " +
                    "Silverback must have control over the offset storing to work properly.");
            }

            EnableAutoOffsetStore = false;
        }

        #region IEquatable

        public bool Equals(KafkaConsumerConfig other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return CommitOffsetEach == other.CommitOffsetEach &&
                   KafkaClientConfigComparer.Compare(this, other);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((KafkaConsumerConfig) obj);
        }

        #endregion
    }
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
}