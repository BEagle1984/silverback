// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using Silverback.Messaging.Proxies;

namespace Silverback.Messaging
{
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public sealed class KafkaConsumerConfig : ConfluentConsumerConfigProxy, IEquatable<KafkaConsumerConfig>
    {
        private const bool KafkaDefaultAutoCommitEnabled = true;

        /// <summary>
        /// Defines the number of message processed before committing the offset to the server.
        /// The most reliable level is 1 but it reduces throughput.
        /// </summary>
        public int CommitOffsetEach { get; set; } = -1;

        /// <summary>
        /// Gets a boolean value indicating whether autocommit is enabled according to the explicit configuration and
        /// Kafka defaults.
        /// </summary>
        public bool IsAutoCommitEnabled => EnableAutoCommit ?? KafkaDefaultAutoCommitEnabled;

        /// <summary>
        /// Specifies whether the consumer has to be automatically restarted if a <see cref="KafkaException"/> is thrown
        /// while polling/consuming.
        /// </summary>
        public bool EnableAutoRecovery { get; set; } = true;

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
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return CommitOffsetEach == other.CommitOffsetEach &&
                   KafkaClientConfigComparer.Compare(this.ConfluentConfig, other.ConfluentConfig);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((KafkaConsumerConfig) obj);
        }

        #endregion
    }
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
}