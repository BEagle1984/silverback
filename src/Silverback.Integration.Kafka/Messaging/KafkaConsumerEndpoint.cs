// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.KafkaEvents;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Sequences.Chunking;

namespace Silverback.Messaging
{
    /// <summary>
    ///     Represents a topic to consume from.
    /// </summary>
    public sealed class KafkaConsumerEndpoint : ConsumerEndpoint, IEquatable<KafkaConsumerEndpoint>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaConsumerEndpoint" /> class.
        /// </summary>
        /// <param name="topicNames">
        ///     The name of the topics.
        /// </param>
        public KafkaConsumerEndpoint(params string[] topicNames)
            : this(topicNames, null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaConsumerEndpoint" /> class.
        /// </summary>
        /// <param name="topicName">
        ///     The name of the topic.
        /// </param>
        /// <param name="clientConfig">
        ///     The <see cref="KafkaClientConfig" /> to be used to initialize the
        ///     <see cref="KafkaConsumerConfig" />.
        /// </param>
        public KafkaConsumerEndpoint(string topicName, KafkaClientConfig? clientConfig = null)
            : this(new[] { topicName }, clientConfig)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaConsumerEndpoint" /> class.
        /// </summary>
        /// <param name="topicNames">
        ///     The name of the topics.
        /// </param>
        /// <param name="clientConfig">
        ///     The <see cref="KafkaClientConfig" /> to be used to initialize the
        ///     <see cref="KafkaConsumerConfig" />.
        /// </param>
        public KafkaConsumerEndpoint(string[] topicNames, KafkaClientConfig? clientConfig = null)
            : base(string.Empty)
        {
            Names = topicNames;

            if (topicNames == null || topicNames.Length == 0)
                return;

            Name = topicNames.Length > 1 ? "[" + string.Join(",", topicNames) + "]" : topicNames[0];

            Configuration = new KafkaConsumerConfig(clientConfig);
        }

        /// <summary>
        ///     Gets the name of the topics.
        /// </summary>
        public IReadOnlyCollection<string> Names { get; }

        /// <summary>
        ///     Gets the event handlers configuration. Can be used to bind some handlers to the Kafka events
        ///     such as partitions revoked/assigned, error, statistics and offsets committed.
        /// </summary>
        public KafkaConsumerEventsHandlers Events { get; } = new();

        /// <summary>
        ///     Gets or sets the Kafka client configuration. This is actually an extension of the configuration
        ///     dictionary provided by the Confluent.Kafka library.
        /// </summary>
        public KafkaConsumerConfig Configuration { get; set; } = new();

        /// <summary>
        ///     Gets or sets a value indicating whether the partitions must be processed independently.
        ///     When <c>true</c> a stream will published per each partition and the sequences
        ///     (<see cref="ChunkSequence" />, <see cref="BatchSequence" />, ...) cannot span across the partitions.
        ///     The default is <c>true</c>.
        /// </summary>
        public bool ProcessPartitionsIndependently { get; set; } = true;

        /// <summary>
        ///     Gets or sets the maximum number of incoming message that can be processed concurrently. Up to a
        ///     message per each subscribed partition can be processed in parallel.
        ///     The default is 10.
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; } = 10;

        /// <summary>
        ///     Gets or sets the maximum number of messages to be consumed and enqueued waiting to be processed.
        ///     When <see cref="ProcessPartitionsIndependently" /> is set to <c>true</c> (default) the limit will be
        ///     applied per partition.
        ///     The default is 1.
        /// </summary>
        public int BackpressureLimit { get; set; } = 1;

        /// <inheritdoc cref="Endpoint.Validate" />
        public override void Validate()
        {
            base.Validate();

            if (Configuration == null)
                throw new EndpointConfigurationException("Configuration cannot be null.");

            if (MaxDegreeOfParallelism < 1)
                throw new EndpointConfigurationException("MaxDegreeOfParallelism must be greater or equal to 1.");

            if (BackpressureLimit < 1)
                throw new EndpointConfigurationException("BackpressureLimit must be greater or equal to 1.");

            Configuration.Validate();
        }

        /// <inheritdoc cref="ConsumerEndpoint.GetUniqueConsumerGroupName" />
        public override string GetUniqueConsumerGroupName() =>
            !string.IsNullOrEmpty(Configuration.GroupId)
                ? Configuration.GroupId
                : Name;

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(KafkaConsumerEndpoint? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return BaseEquals(other) && Equals(Configuration, other.Configuration);
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

            return Equals((KafkaConsumerEndpoint)obj);
        }

        /// <inheritdoc cref="object.GetHashCode" />
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode", Justification = "Protected set is not abused")]
        public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);
    }
}
