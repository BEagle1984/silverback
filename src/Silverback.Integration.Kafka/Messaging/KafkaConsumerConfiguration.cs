// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using Silverback.Collections;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Sequences.Chunking;

namespace Silverback.Messaging;

/// <summary>
///     The Kafka consumer configuration.
/// </summary>
public sealed record KafkaConsumerConfiguration : ConsumerConfiguration
{
    private readonly bool _processPartitionsIndependently = true;

    private readonly IValueReadOnlyCollection<TopicPartitionOffset> _topicPartitions = ValueReadOnlyCollection.Empty<TopicPartitionOffset>();

    private readonly Func<IReadOnlyCollection<TopicPartition>, IEnumerable<TopicPartitionOffset>>? _partitionOffsetsProvider;

    /// <summary>
    ///     <para>
    ///         Gets the topics and partitions to be consumed.
    ///     </para>
    ///     <para>
    ///         Setting the partition to <see cref="Partition.Any" /> for a topic will cause it to be subscribed to get a partition
    ///         assignment from the message broker, unless a <see cref="PartitionOffsetsProvider" /> is set. An
    ///         <see cref="IKafkaPartitionsAssignedCallback" /> can be registered to interact with the partitions assignment process.
    ///     </para>
    ///     <para>
    ///         Setting the partition to <see cref="Partition.Any" /> for a topic and setting a <see cref="PartitionOffsetsProvider" />
    ///         will cause the consumer to retrieve the topics metadata, collect all available partitions and call the specified resolver
    ///         function. The result of the function will be used as static partition assignment for the consumer.
    ///     </para>
    ///     <para>
    ///         Setting the offset to <see cref="Offset.Unset" /> means that the offset stored on the message broker will be used. If no
    ///         offset was stored on the server the <c>Configuration.AutoOffsetReset</c> property will determine where to start consuming.
    ///     </para>
    /// </summary>
    public IValueReadOnlyCollection<TopicPartitionOffset> TopicPartitions
    {
        get => _topicPartitions;
        init
        {
            _topicPartitions = value;

            if (value != null)
            {
                IsStaticAssignment = GetIsStaticAssignment();
                RawName = string.Join(",", value.Select(topicPartitionOffset => topicPartitionOffset.TopicPartition.ToDisplayString()));
            }
        }
    }

    /// <summary>
    ///     Gets a function that receives all available <see cref="TopicPartition" /> for the topic(s) and returns the collection of
    ///     <see cref="TopicPartitionOffset" /> containing the partitions to be consumed and their starting offsets.
    /// </summary>
    public Func<IReadOnlyCollection<TopicPartition>, IEnumerable<TopicPartitionOffset>>? PartitionOffsetsProvider
    {
        get => _partitionOffsetsProvider;
        init
        {
            _partitionOffsetsProvider = value;
            IsStaticAssignment = GetIsStaticAssignment();
        }
    }

    /// <summary>
    ///     Gets the Kafka client configuration. This is actually an extension of the configuration dictionary provided by the
    ///     Confluent.Kafka library.
    /// </summary>
    public KafkaClientConsumerConfiguration Client { get; init; } = new();

    /// <summary>
    ///     Gets a value indicating whether the partitions must be processed independently.
    ///     When <c>true</c> a stream will published per each partition and the sequences (<see cref="ChunkSequence" />,
    ///     <see cref="BatchSequence" />, ...) cannot span across the partitions.
    ///     The default is <c>true</c>.
    /// </summary>
    /// <remarks>
    ///     Settings this value to <c>false</c> implicitly sets the <see cref="MaxDegreeOfParallelism" /> to 1.
    /// </remarks>
    public bool ProcessPartitionsIndependently
    {
        get => _processPartitionsIndependently;
        init
        {
            _processPartitionsIndependently = value;

            if (!value)
                MaxDegreeOfParallelism = 1;
        }
    }

    /// <summary>
    ///     Gets the maximum number of incoming message that can be processed concurrently. Up to a
    ///     message per each subscribed partition can be processed in parallel when processing them independently.
    ///     The default is 100.
    /// </summary>
    public int MaxDegreeOfParallelism { get; init; } = 100;

    /// <summary>
    ///     Gets the maximum number of messages to be consumed and enqueued waiting to be processed.
    ///     When <see cref="ProcessPartitionsIndependently" /> is set to <c>true</c> (default) the limit will be applied per partition.
    ///     The default is 2.
    /// </summary>
    public int BackpressureLimit { get; init; } = 2;

    internal bool IsStaticAssignment { get; init; }

    /// <inheritdoc cref="ConsumerConfiguration.ValidateCore" />
    protected override void ValidateCore()
    {
        base.ValidateCore();

        if (Client == null)
        {
            throw new EndpointConfigurationException(
                "The client configuration is required.",
                Client,
                nameof(Client));
        }

        if (MaxDegreeOfParallelism < 1)
        {
            throw new EndpointConfigurationException(
                "The specified degree of parallelism must be greater or equal to 1.",
                MaxDegreeOfParallelism,
                nameof(MaxDegreeOfParallelism));
        }

        if (MaxDegreeOfParallelism > 1 && !_processPartitionsIndependently)
        {
            throw new EndpointConfigurationException(
                $"{nameof(MaxDegreeOfParallelism)} cannot be greater than 1 when the partitions aren't processed independently.",
                MaxDegreeOfParallelism,
                nameof(MaxDegreeOfParallelism));
        }

        if (BackpressureLimit < 1)
        {
            throw new EndpointConfigurationException(
                "The backpressure limit must be greater or equal to 1.",
                BackpressureLimit,
                nameof(BackpressureLimit));
        }

        ValidateTopicPartitions();

        Client.Validate(IsStaticAssignment);
    }

    private void ValidateTopicPartitions()
    {
        if (TopicPartitions == null || TopicPartitions.Count == 0)
        {
            throw new EndpointConfigurationException(
                "At least 1 topic must be specified.",
                TopicPartitions,
                nameof(TopicPartitions));
        }

        if (TopicPartitions.Any(topicPartition => string.IsNullOrEmpty(topicPartition.Topic)))
        {
            throw new EndpointConfigurationException(
                "The topic name cannot be null or empty.",
                TopicPartitions,
                nameof(TopicPartitions));
        }

        if (TopicPartitions.Any(topicPartition => topicPartition.Partition.Value < Partition.Any))
        {
            throw new EndpointConfigurationException(
                "The partition must be a value greater or equal to 0, or Partition.Any.",
                TopicPartitions,
                nameof(TopicPartitions));
        }

        if (PartitionOffsetsProvider != null &&
            TopicPartitions.Any(topicPartition => topicPartition.Partition != Partition.Any))
        {
            throw new EndpointConfigurationException(
                $"Cannot specify a {nameof(PartitionOffsetsProvider)} if the partitions are already specified. " +
                $"Use Partition.Any when specifying the topic partition or remove the resolver.",
                TopicPartitions,
                nameof(TopicPartitions));
        }

        foreach (IGrouping<string, TopicPartitionOffset> groupedPartitions in TopicPartitions.GroupBy(topicPartition => topicPartition.Topic))
        {
            List<Partition> partitions = groupedPartitions.Select(topicPartition => topicPartition.Partition).ToList();
            List<TopicPartition> topicPartitionsWithoutOffset = groupedPartitions.Select(offset => offset.TopicPartition).ToList();

            if (topicPartitionsWithoutOffset.Count != topicPartitionsWithoutOffset.Distinct().Count())
            {
                throw new EndpointConfigurationException(
                    "Each partition must be specified only once.",
                    TopicPartitions,
                    nameof(TopicPartitions));
            }

            if (partitions.Any(partition => partition == Partition.Any) && partitions.Count > 1)
            {
                throw new EndpointConfigurationException(
                    "Cannot mix Partition.Any with a specific partition assignment for the same topic.",
                    TopicPartitions,
                    nameof(TopicPartitions));
            }
        }

        if (TopicPartitions.Any(topicPartition => topicPartition.Partition == Partition.Any) &&
            TopicPartitions.Any(topicPartition => topicPartition.Partition != Partition.Any) &&
            PartitionOffsetsProvider == null)
        {
            throw new EndpointConfigurationException(
                "Cannot mix static partition assignments and subscriptions in the same consumer." +
                "A PartitionOffsetsProvider is required when mixing Partition.Any with static partition assignments.");
        }
    }

    private bool GetIsStaticAssignment() =>
        PartitionOffsetsProvider != null ||
        TopicPartitions == null || TopicPartitions.Any(topicPartition => topicPartition.Partition != Partition.Any);
}
