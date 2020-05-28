// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Confluent.Kafka;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     The event published when a new consumer group partition assignment has been received by a consumer.
    /// </summary>
    /// <remarks>
    ///     Corresponding to each of this events there will be a <see cref="KafkaPartitionsRevokedEvent" />.
    /// </remarks>
    public class KafkaPartitionsAssignedEvent : IKafkaEvent
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaPartitionsAssignedEvent" /> class.
        /// </summary>
        /// <param name="partitions">
        ///     The collection of <see cref="Confluent.Kafka.TopicPartitionOffset" /> representing the assigned
        ///     partitions.
        /// </param>
        /// <param name="memberId">
        ///     The (dynamic) group member id of this consumer (as set by the broker).
        /// </param>
        public KafkaPartitionsAssignedEvent(
            IReadOnlyCollection<TopicPartition> partitions,
            string memberId)
        {
            Partitions = partitions.Select(
                    partition =>
                        new TopicPartitionOffset(
                            partition,
                            Offset.Unset))
                .ToList();
            MemberId = memberId;
        }

        /// <summary>
        ///     Gets the (dynamic) group member id of this consumer (as set by the broker).
        /// </summary>
        public string MemberId { get; }

        /// <summary>
        ///     <para>
        ///         Gets or sets the collection of <see cref="Confluent.Kafka.TopicPartitionOffset" /> representing
        ///         the assigned partitions.
        ///     </para>
        ///     <para>
        ///         The collection can be modified to set the actual partitions to consume from and the start
        ///         offsets.
        ///     </para>
        /// </summary>
        [SuppressMessage("", "CA2227", Justification = "Easier to modify if writable")]
        public ICollection<TopicPartitionOffset> Partitions { get; set; }
    }
}
