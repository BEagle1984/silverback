// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.KafkaEvents;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging
{
    /// <summary>
    ///     Represents a topic to produce to.
    /// </summary>
    public sealed class KafkaProducerEndpoint : ProducerEndpoint, IEquatable<KafkaProducerEndpoint>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaProducerEndpoint" /> class.
        /// </summary>
        /// <param name="name">
        ///     The name of the topic.
        /// </param>
        /// <param name="clientConfig">
        ///     The <see cref="KafkaClientConfig" /> to be used to initialize the
        ///     <see cref="KafkaProducerConfig" />.
        /// </param>
        public KafkaProducerEndpoint(string name, KafkaClientConfig? clientConfig = null)
            : base(name)
        {
            Configuration = new KafkaProducerConfig(clientConfig);
            TopicPartition = new TopicPartition(name, Partition.Any);
        }

        /// <summary>
        ///     Gets the Kafka event handlers configuration.
        /// </summary>
        public KafkaProducerEventsHandlers Events { get; } = new();

        /// <summary>
        ///     Gets or sets the Kafka client configuration. This is actually an extension of the configuration
        ///     dictionary provided by the Confluent.Kafka library.
        /// </summary>
        public KafkaProducerConfig Configuration { get; set; }

        /// <summary>
        ///     Gets or sets the target partition. When not set the partition is automatically derived from the message
        ///     key (use <see cref="KafkaKeyMemberAttribute" /> to specify a message key, otherwise a random one will be
        ///     generated).
        /// </summary>
        public Partition Partition
        {
            get => TopicPartition.Partition;
            set => TopicPartition = new TopicPartition(Name, value);
        }

        /// <summary>
        ///     Gets the <see cref="TopicPartition"/> representing this endpoint.
        /// </summary>
        public TopicPartition TopicPartition { get; private set; }

        /// <inheritdoc cref="ProducerEndpoint.Validate" />
        public override void Validate()
        {
            base.Validate();

            if (Configuration == null)
                throw new EndpointConfigurationException("Configuration cannot be null.");

            if (Partition.Value < -1)
            {
                throw new EndpointConfigurationException(
                    "Partition should be set to an index greater or equal to 0, or Partition.Any.");
            }

            Configuration.Validate();
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(KafkaProducerEndpoint? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return BaseEquals(other) &&
                   Equals(Configuration, other.Configuration) &&
                   Partition == other.Partition;
        }

        /// <inheritdoc cref="object.Equals(object)" />
        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != GetType())
                return false;

            return Equals((KafkaProducerEndpoint)obj);
        }

        /// <inheritdoc cref="object.GetHashCode" />
        [SuppressMessage(
            "ReSharper",
            "NonReadonlyMemberInGetHashCode",
            Justification = "Protected set is not abused")]
        public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);
    }
}
