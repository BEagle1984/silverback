// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     The event fired when a new consumer group partition assignment has been received by a consumer.
    /// </summary>
    /// <remarks>
    ///     Corresponding to each of this events there will be a <see cref="KafkaPartitionsRevokedEvent" />.
    /// </remarks>
    public class KafkaPartitionsAssignedEvent : IKafkaEvent
    {
        public KafkaPartitionsAssignedEvent(
            IReadOnlyCollection<Confluent.Kafka.TopicPartition> partitions,
            string memberId)
        {
            Partitions = partitions;
            MemberId = memberId;
        }

        /// <summary>
        ///     Gets the collection of <see cref="Confluent.Kafka.TopicPartition" /> that have been assigned.
        /// </summary>
        public IReadOnlyCollection<Confluent.Kafka.TopicPartition> Partitions { get; }

        /// <summary>
        ///     Gets the (dynamic) group member id of this consumer (as set by the broker).
        /// </summary>
        public string MemberId { get; }
    }
}