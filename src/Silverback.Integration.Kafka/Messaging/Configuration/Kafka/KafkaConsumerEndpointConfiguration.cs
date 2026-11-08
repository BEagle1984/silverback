// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Silverback.Collections;
using Silverback.Messaging.Broker.Callbacks;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     The Kafka consumer endpoint configuration.
/// </summary>
public sealed record KafkaConsumerEndpointConfiguration : ConsumerEndpointConfiguration
{
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
    ///         A topic starting with <c>^</c> is a regular expression and subscribes all matching topics, including the ones created
    ///         later. The partition must be <see cref="Partition.Any" />. A message is routed to the endpoint whose topic name matches
    ///         exactly, or else to the first endpoint whose pattern matches it. The pattern should match the whole topic name (e.g.
    ///         <c>^orders-.*</c>), since the broker requires it with the consumer group protocol.
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
        get;
        init
        {
            field = value;

            if (value == null)
                return;

            IsStaticAssignment = GetIsStaticAssignment();
            RawName = string.Join(",", value.Select(topicPartitionOffset => topicPartitionOffset.TopicPartition.ToDisplayString()));
        }
    } = ValueReadOnlyCollection.Empty<TopicPartitionOffset>();

    /// <summary>
    ///     Gets a function that receives all available <see cref="TopicPartition" /> for the topic(s) and returns the collection of
    ///     <see cref="TopicPartitionOffset" /> containing the partitions to be consumed and their starting offsets.
    /// </summary>
    public Func<IReadOnlyCollection<TopicPartition>, ValueTask<IEnumerable<TopicPartitionOffset>>>? PartitionOffsetsProvider
    {
        get;
        init
        {
            field = value;
            IsStaticAssignment = GetIsStaticAssignment();
        }
    }

    /// <summary>
    ///     Gets a value indicating whether the consumer is configured with a static partition assignment.
    /// </summary>
    internal bool IsStaticAssignment { get; init; }

    /// <inheritdoc cref="ConsumerEndpointConfiguration.ValidateCore" />
    protected override void ValidateCore()
    {
        base.ValidateCore();

        if (TopicPartitions == null || TopicPartitions.Count == 0)
            throw new BrokerConfigurationException("At least 1 topic must be specified.");

        if (TopicPartitions.Any(topicPartition => string.IsNullOrEmpty(topicPartition.Topic)))
            throw new BrokerConfigurationException("The topic name cannot be null or empty.");

        if (TopicPartitions.Any(topicPartition => topicPartition.Partition.Value < Partition.Any))
            throw new BrokerConfigurationException("The partition must be a value greater or equal to 0, or Partition.Any.");

        if (PartitionOffsetsProvider != null && TopicPartitions.Any(topicPartition => topicPartition.Partition != Partition.Any))
        {
            throw new BrokerConfigurationException(
                $"Cannot specify a {nameof(PartitionOffsetsProvider)} if the partitions are already specified. " +
                "Use Partition.Any when specifying the topic partition or remove the resolver.");
        }

        ValidateTopicPatterns();
        ValidateTopicPartitions();
        ValidatePartitionsAssignment();
    }

    private void ValidateTopicPatterns()
    {
        foreach (TopicPartitionOffset topicPartition in TopicPartitions.Where(topicPartition => KafkaTopicPattern.IsPattern(topicPartition.Topic)))
        {
            if (topicPartition.Partition != Partition.Any || PartitionOffsetsProvider != null)
            {
                throw new BrokerConfigurationException(
                    $"Cannot assign the partitions of the topic pattern '{topicPartition.Topic}'. " +
                    "The topics matching a pattern can only be subscribed, using Partition.Any and no partitions provider.");
            }

            try
            {
                _ = KafkaTopicPattern.Create(topicPartition.Topic);
            }
            catch (ArgumentException ex)
            {
                throw new BrokerConfigurationException($"The topic pattern '{topicPartition.Topic}' is not a valid regular expression.", ex);
            }
        }
    }

    private void ValidateTopicPartitions()
    {
        foreach (IGrouping<string, TopicPartitionOffset> groupedPartitions in TopicPartitions.GroupBy(topicPartition => topicPartition.Topic))
        {
            List<Partition> partitions = [.. groupedPartitions.Select(topicPartition => topicPartition.Partition)];
            List<TopicPartition> topicPartitionsWithoutOffset = [.. groupedPartitions.Select(offset => offset.TopicPartition)];

            if (topicPartitionsWithoutOffset.Count != topicPartitionsWithoutOffset.Distinct().Count())
            {
                throw new BrokerConfigurationException("Each partition must be specified only once.");
            }

            if (partitions.Exists(partition => partition == Partition.Any) && partitions.Count > 1)
            {
                throw new BrokerConfigurationException("Cannot mix Partition.Any with a specific partition assignment for the same topic.");
            }
        }
    }

    private void ValidatePartitionsAssignment()
    {
        if (TopicPartitions.Any(topicPartition => topicPartition.Partition == Partition.Any) &&
            TopicPartitions.Any(topicPartition => topicPartition.Partition != Partition.Any) &&
            PartitionOffsetsProvider == null)
        {
            throw new BrokerConfigurationException(
                "Cannot mix static partition assignments and subscriptions in the same consumer." +
                "A PartitionOffsetsProvider is required when mixing Partition.Any with static partition assignments.");
        }
    }

    private bool GetIsStaticAssignment() =>
        PartitionOffsetsProvider != null || TopicPartitions.Any(topicPartition => topicPartition.Partition != Partition.Any);
}
