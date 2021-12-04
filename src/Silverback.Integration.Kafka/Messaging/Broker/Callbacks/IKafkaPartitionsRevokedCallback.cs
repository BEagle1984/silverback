// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Callbacks;

/// <summary>
///     Declares the <see cref="OnPartitionsRevoked" /> event handler.
/// </summary>
public interface IKafkaPartitionsRevokedCallback : IBrokerCallback
{
    /// <summary>
    ///     Called immediately prior to a group partition assignment being revoked.
    /// </summary>
    /// <param name="topicPartitionsOffset">
    ///     A collection of <see cref="TopicPartitionOffset" /> representing the the set of partitions the consumer
    ///     is currently assigned to, and the current position of the consumer on each of these partitions.
    /// </param>
    /// <param name="consumer">
    ///     The related consumer instance.
    /// </param>
    void OnPartitionsRevoked(
        IReadOnlyCollection<TopicPartitionOffset> topicPartitionsOffset,
        KafkaConsumer consumer);
}
