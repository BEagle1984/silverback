// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Callbacks;

/// <summary>
///     Declares the <see cref="OnPartitionsAssigned" /> event handler.
/// </summary>
public interface IKafkaPartitionsAssignedCallback : IBrokerClientCallback
{
    /// <summary>
    ///     Called when a new consumer group partition assignment has been received by the consumer.
    /// </summary>
    /// <param name="topicPartitions">
    ///     A collection of <see cref="TopicPartition" /> representing the assigned partitions.
    /// </param>
    /// <param name="consumer">
    ///     The related consumer instance.
    /// </param>
    /// <returns>
    ///     <para>
    ///         Optionally returns the actual partitions to consume from and start offsets are specified by the
    ///         return value of the this set of partitions is not required to match the assignment provided by the
    ///         consumer group, but typically will. Partition offsets may be a specific offset, or special value (
    ///         <c>Beginning</c>, <c>End</c> or <c>Unset</c>). If <c>Unset</c>, consumption will resume from the last
    ///         committed offset for each partition, or if there is no committed offset, in accordance with the
    ///         <c>auto.offset.reset</c> configuration property.
    ///     </para>
    ///     <para>
    ///         When <c>null</c> the partitions assignment from the broker is taken and all offsets will be
    ///         considered <c>Unset</c>.
    ///     </para>
    /// </returns>
    IEnumerable<TopicPartitionOffset>? OnPartitionsAssigned(IReadOnlyCollection<TopicPartition> topicPartitions, KafkaConsumer consumer);
}
