// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Callbacks
{
    /// <summary>
    ///     Declares the <see cref="OnEndOfTopicPartitionReached" /> event handler.
    /// </summary>
    public interface IKafkaEndOfTopicPartitionReachedCallback : IBrokerCallback
    {
        /// <summary>
        ///     Called to report the end of partition has reached.
        /// </summary>
        /// <param name="topicPartition">
        ///    The related topicPartition.
        /// </param>
        /// <param name="consumer">
        ///    The related consumer.
        /// </param>
        void OnEndOfTopicPartitionReached(TopicPartition topicPartition, KafkaConsumer consumer);
    }
}
