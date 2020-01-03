// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     The event fired prior to a group partition assignment being revoked.
    /// </summary>
    /// <remarks>
    ///     Corresponding to each of this events there will be a <see cref="KafkaPartitionsAssignedEvent" />.
    /// </remarks>
    public class KafkaPartitionsRevokedEvent : IKafkaEvent
    {
        public KafkaPartitionsRevokedEvent(
            IReadOnlyCollection<Confluent.Kafka.TopicPartitionOffset> partitions,
            string memberId)
        {
            Partitions = partitions;
            MemberId = memberId;
        }

        /// <summary>
        ///     Gets the collection of <see cref="Confluent.Kafka.TopicPartitionOffset" /> representing the set of partitions the
        ///     consumer is currently assigned to, and the current position of the consumer on each of these
        ///     partitions.
        /// </summary>
        public IReadOnlyCollection<Confluent.Kafka.TopicPartitionOffset> Partitions { get; }

        /// <summary>
        ///     Gets the (dynamic) group member id of this consumer (as set by the broker).
        /// </summary>
        public string MemberId { get; }
    }
}