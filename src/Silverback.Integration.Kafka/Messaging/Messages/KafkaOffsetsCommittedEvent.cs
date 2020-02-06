// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     The event fired to report the result of the offset commits.
    /// </summary>
    /// <remarks>
    ///     Possible error conditions:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 Entire request failed: <c>Error</c> is set, but not per-partition errors.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 All partitions failed: <c>Error</c> is set to the value of the last failed partition,
    ///                 but each partition may have different errors.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 Some partitions failed: global <c>Error</c> is success (
    ///                 <see cref="Confluent.Kafka.ErrorCode.NoError" />).
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public class KafkaOffsetsCommittedEvent : IKafkaEvent
    {
        public KafkaOffsetsCommittedEvent(
            Confluent.Kafka.CommittedOffsets committedOffsets)
            : this(committedOffsets?.Offsets.ToList(), committedOffsets?.Error)
        {
        }

        public KafkaOffsetsCommittedEvent(
            IReadOnlyCollection<Confluent.Kafka.TopicPartitionOffsetError> offsets,
            Confluent.Kafka.Error error = null)
        {
            Offsets = offsets ?? Array.Empty<Confluent.Kafka.TopicPartitionOffsetError>();
            Error = error ?? new Confluent.Kafka.Error(Confluent.Kafka.ErrorCode.NoError);
        }

        /// <summary>
        ///     Gets the per-partition offsets and success or error information.
        /// </summary>
        public IReadOnlyCollection<Confluent.Kafka.TopicPartitionOffsetError> Offsets { get; }

        /// <summary>
        ///     Gets the overall operation success or error information.
        /// </summary>
        public Confluent.Kafka.Error Error { get; }
    }
}