// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Silverback.Collections;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Kafka;

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
    ///         assignment from the message broker. An <see cref="IKafkaPartitionsAssignedCallback" /> can be registered to interact
    ///         with the partitions assignment process. Otherwise, the specified partitions are statically assigned.
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

        ValidateTopicPartitions();
        ValidatePartitionsAssignment();
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
            TopicPartitions.Any(topicPartition => topicPartition.Partition != Partition.Any))
        {
            throw new BrokerConfigurationException("Cannot mix static partition assignments and subscriptions in the same consumer.");
        }
    }

    private bool GetIsStaticAssignment() =>
        TopicPartitions.Any(topicPartition => topicPartition.Partition != Partition.Any);
}
