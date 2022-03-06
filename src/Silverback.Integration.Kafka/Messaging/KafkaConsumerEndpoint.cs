﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Confluent.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Sequences.Chunking;

namespace Silverback.Messaging
{
    /// <summary>
    ///     Represents a topic to consume from.
    /// </summary>
    public sealed class KafkaConsumerEndpoint : ConsumerEndpoint, IEquatable<KafkaConsumerEndpoint>
    {
        private bool _processPartitionsIndependently = true;

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
            if (topicNames == null || topicNames.Length == 0)
                return;

            Init(topicNames, clientConfig);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaConsumerEndpoint" /> class.
        /// </summary>
        /// <param name="topicPartitions">
        ///     The topics and partitions to be consumed.
        /// </param>
        public KafkaConsumerEndpoint(params TopicPartition[] topicPartitions)
            : this(topicPartitions, null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaConsumerEndpoint" /> class.
        /// </summary>
        /// <param name="topicPartitions">
        ///     The topics and partitions to be consumed.
        /// </param>
        /// <param name="clientConfig">
        ///     The <see cref="KafkaClientConfig" /> to be used to initialize the
        ///     <see cref="KafkaConsumerConfig" />.
        /// </param>
        public KafkaConsumerEndpoint(
            IEnumerable<TopicPartition> topicPartitions,
            KafkaClientConfig? clientConfig = null)
            : this(
                topicPartitions?.Select(topicPartition => new TopicPartitionOffset(topicPartition, Offset.Unset))!,
                clientConfig)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaConsumerEndpoint" /> class.
        /// </summary>
        /// <param name="topicPartitions">
        ///     The topics and partitions to be consumed and the starting offset.
        /// </param>
        public KafkaConsumerEndpoint(params TopicPartitionOffset[] topicPartitions)
            : this(topicPartitions, null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaConsumerEndpoint" /> class.
        /// </summary>
        /// <param name="topicPartitions">
        ///     The topics and partitions to be consumed.
        /// </param>
        /// <param name="clientConfig">
        ///     The <see cref="KafkaClientConfig" /> to be used to initialize the
        ///     <see cref="KafkaConsumerConfig" />.
        /// </param>
        public KafkaConsumerEndpoint(
            IEnumerable<TopicPartitionOffset> topicPartitions,
            KafkaClientConfig? clientConfig = null)
            : base(string.Empty)
        {
            if (topicPartitions == null)
                return;

            TopicPartitions = topicPartitions.ToArray();

            var topicNames = TopicPartitions
                .Select(topicPartitionOffset => topicPartitionOffset.Topic)
                .Distinct()
                .ToArray();

            if (topicNames.Length == 0)
                return;

            Init(topicNames, clientConfig);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaConsumerEndpoint" /> class.
        /// </summary>
        /// <param name="topicName">
        ///     The name of the topic.
        /// </param>
        /// <param name="topicPartitionsResolver">
        ///     The function that returns the topics and partitions to be consumed.
        /// </param>
        /// <param name="clientConfig">
        ///     The <see cref="KafkaClientConfig" /> to be used to initialize the
        ///     <see cref="KafkaConsumerConfig" />.
        /// </param>
        public KafkaConsumerEndpoint(
            string topicName,
            Func<IReadOnlyCollection<TopicPartition>, IEnumerable<TopicPartition>> topicPartitionsResolver,
            KafkaClientConfig? clientConfig = null)
            : this(
                new[] { topicName },
                topicPartitionsResolver,
                clientConfig)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaConsumerEndpoint" /> class.
        /// </summary>
        /// <param name="topicNames">
        ///     The name of the topics.
        /// </param>
        /// <param name="topicPartitionsResolver">
        ///     The function that returns the topics and partitions to be consumed.
        /// </param>
        /// <param name="clientConfig">
        ///     The <see cref="KafkaClientConfig" /> to be used to initialize the
        ///     <see cref="KafkaConsumerConfig" />.
        /// </param>
        public KafkaConsumerEndpoint(
            string[] topicNames,
            Func<IReadOnlyCollection<TopicPartition>, IEnumerable<TopicPartition>> topicPartitionsResolver,
            KafkaClientConfig? clientConfig = null)
            : this(
                topicNames,
                topicPartitions => topicPartitionsResolver(topicPartitions)
                    .Select(topicPartition => new TopicPartitionOffset(topicPartition, Offset.Unset)),
                clientConfig)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaConsumerEndpoint" /> class.
        /// </summary>
        /// <param name="topicName">
        ///     The name of the topic.
        /// </param>
        /// <param name="topicPartitionsResolver">
        ///     The function that returns the topics and partitions to be consumed.
        /// </param>
        /// <param name="clientConfig">
        ///     The <see cref="KafkaClientConfig" /> to be used to initialize the
        ///     <see cref="KafkaConsumerConfig" />.
        /// </param>
        public KafkaConsumerEndpoint(
            string topicName,
            Func<IReadOnlyCollection<TopicPartition>, IEnumerable<TopicPartitionOffset>>
                topicPartitionsResolver,
            KafkaClientConfig? clientConfig = null)
            : this(
                new[] { topicName },
                topicPartitionsResolver,
                clientConfig)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaConsumerEndpoint" /> class.
        /// </summary>
        /// <param name="topicNames">
        ///     The name of the topics.
        /// </param>
        /// <param name="topicPartitionsResolver">
        ///     The function that returns the topics and partitions to be consumed.
        /// </param>
        /// <param name="clientConfig">
        ///     The <see cref="KafkaClientConfig" /> to be used to initialize the
        ///     <see cref="KafkaConsumerConfig" />.
        /// </param>
        public KafkaConsumerEndpoint(
            string[] topicNames,
            Func<IReadOnlyCollection<TopicPartition>, IEnumerable<TopicPartitionOffset>>
                topicPartitionsResolver,
            KafkaClientConfig? clientConfig = null)
            : this(topicNames, clientConfig)
        {
            if (topicPartitionsResolver == null)
                return;

            TopicPartitionsResolver = topicPartitionsResolver;
        }

        /// <summary>
        ///     <para>
        ///         Gets the function that returns the topics and partitions to be consumed.
        ///     </para>
        ///     <para>
        ///         If both <see cref="TopicPartitions" /> and <see cref="TopicPartitionsResolver" /> are <c>null</c> the
        ///         topics from the <see cref="Names" /> property will be subscribed and the partitions will be
        ///         automatically assigned by the broker.
        ///     </para>
        ///     <para>
        ///         If an empty collection is returned by the function no partition will be consumed.
        ///     </para>
        /// </summary>
        public Func<IReadOnlyCollection<TopicPartition>, IEnumerable<TopicPartitionOffset>>?
            TopicPartitionsResolver { get; }

        /// <summary>
        ///     <para>
        ///         Gets the topics and partitions to be consumed.
        ///     </para>
        ///     <para>
        ///         If both <see cref="TopicPartitions" /> and <see cref="TopicPartitionsResolver" /> are <c>null</c> the
        ///         topics from the <see cref="Names" /> property will be subscribed and the partitions will be
        ///         automatically assigned by the broker.
        ///     </para>
        ///     <para>
        ///         If the collection is empty no partition will be consumed.
        ///     </para>
        /// </summary>
        public IReadOnlyCollection<TopicPartitionOffset>? TopicPartitions { get; internal set; }

        /// <summary>
        ///     Gets the name of the topics.
        /// </summary>
        public IReadOnlyCollection<string> Names { get; private set; } = Array.Empty<string>();

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
        /// <remarks>
        ///     Settings this value to <c>false</c> implicitly sets the <see cref="MaxDegreeOfParallelism"/> to 1.
        /// </remarks>
        public bool ProcessPartitionsIndependently
        {
            get => _processPartitionsIndependently;
            set
            {
                _processPartitionsIndependently = value;

                if (!value)
                    MaxDegreeOfParallelism = 1;
            }
        }

        /// <summary>
        ///     Gets or sets the maximum number of incoming message that can be processed concurrently. Up to a
        ///     message per each subscribed partition can be processed in parallel when processing them independently.
        ///     The default is 100.
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; } = 100;

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

            if (MaxDegreeOfParallelism > 1 && !_processPartitionsIndependently)
            {
                throw new EndpointConfigurationException(
                    "MaxDegreeOfParallelism cannot be greater than 1 when the partitions aren't " +
                    "processed independently.");
            }

            if (BackpressureLimit < 1)
                throw new EndpointConfigurationException("BackpressureLimit must be greater or equal to 1.");

            Configuration.Validate();
        }

        /// <inheritdoc cref="ConsumerEndpoint.GetUniqueConsumerGroupName" />
        public override string GetUniqueConsumerGroupName() =>
            !string.IsNullOrEmpty(Configuration.GroupId)
                ? Configuration.GroupId
                : DisplayName;

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
        [SuppressMessage(
            "ReSharper",
            "NonReadonlyMemberInGetHashCode",
            Justification = "Protected set is not abused")]
        public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);

        private void Init(string[] topicNames, KafkaClientConfig? clientConfig)
        {
            Name = topicNames.Length > 1 ? "[" + string.Join(",", topicNames) + "]" : topicNames[0];
            Names = topicNames;

            Configuration = new KafkaConsumerConfig(clientConfig);
        }
    }
}
