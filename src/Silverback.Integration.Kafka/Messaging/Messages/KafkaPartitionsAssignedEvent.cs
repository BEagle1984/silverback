// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;

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
            Partitions = partitions.Select(partition =>
                new Confluent.Kafka.TopicPartitionOffset(
                    partition, 
                    Confluent.Kafka.Offset.Unset))
                .ToList();
            MemberId = memberId;
        }

        /// <summary>
        ///     <para>
        ///         Gets or sets the collection of <see cref="Confluent.Kafka.TopicPartitionOffset" /> representing the
        ///         assigned partitions.
        ///     </para>
        ///     <para>
        ///         The collection can be modified to set the actual partitions to consume from and the start offsets.
        ///     </para>
        /// </summary>
        public ICollection<Confluent.Kafka.TopicPartitionOffset> Partitions { get; set; }

        /// <summary>
        ///     Gets the (dynamic) group member id of this consumer (as set by the broker).
        /// </summary>
        public string MemberId { get; }
    }
}