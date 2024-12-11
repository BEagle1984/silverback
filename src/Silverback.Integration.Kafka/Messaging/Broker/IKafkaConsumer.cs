// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Confluent.Kafka;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;

namespace Silverback.Messaging.Broker;

/// <inheritdoc cref="IConsumer" />
public interface IKafkaConsumer : IConsumer
{
    /// <inheritdoc cref="Consumer{TIdentifier}.Client" />
    new IConfluentConsumerWrapper Client { get; }

    /// <summary>
    ///     Gets the consumer configuration.
    /// </summary>
    KafkaConsumerConfiguration Configuration { get; }

    /// <inheritdoc cref="Consumer{TIdentifier}.EndpointsConfiguration" />
    new IReadOnlyCollection<KafkaConsumerEndpointConfiguration> EndpointsConfiguration { get; }

    /// <summary>
    ///     Pauses the consumption of the specified partitions.
    /// </summary>
    /// <param name="partitions">
    ///     The list of <see cref="TopicPartition" /> to be paused.
    /// </param>
    void Pause(IEnumerable<TopicPartition> partitions);

    /// <summary>
    ///     Resumes the consumption of the specified partitions.
    /// </summary>
    /// <param name="partitions">
    ///     The list of <see cref="TopicPartition" /> to be paused.
    /// </param>
    void Resume(IEnumerable<TopicPartition> partitions);

    /// <summary>
    ///     Seeks the specified partition to the specified offset.
    /// </summary>
    /// <param name="topicPartitionOffset">
    ///     The offset.
    /// </param>
    void Seek(TopicPartitionOffset topicPartitionOffset);
}
