// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;
using Silverback.Messaging.Configuration.Kafka;

namespace Silverback.Messaging.Broker.Callbacks;

/// <summary>
///     Declares the <see cref="OnEndOfTopicPartitionReached" /> event handler.
/// </summary>
/// <remarks>
///     The <see cref="ConfluentConsumerConfigProxy.EnablePartitionEof" /> must be set to <c>true</c> in the
///     <see cref="KafkaClientConsumerConfiguration" />, otherwise the underlying library will not emit this event.
/// </remarks>
public interface IKafkaPartitionEofCallback : IBrokerCallback
{
    /// <summary>
    ///     Called to report that the end of a partition has been reached, meaning that it has been completely
    ///     consumed.
    /// </summary>
    /// <param name="topicPartition">
    ///     The topic partition.
    /// </param>
    /// <param name="consumer">
    ///     The related consumer.
    /// </param>
    /// <remarks>
    ///     The <see cref="ConfluentConsumerConfigProxy.EnablePartitionEof" /> must be set to <c>true</c> in the
    ///     <see cref="KafkaClientConsumerConfiguration" />, otherwise the underlying library will not emit this event.
    /// </remarks>
    void OnEndOfTopicPartitionReached(TopicPartition topicPartition, KafkaConsumer consumer);
}
