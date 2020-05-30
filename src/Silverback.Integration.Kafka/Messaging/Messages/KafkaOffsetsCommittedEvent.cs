// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     The event published to report the result of the offset commits.
    /// </summary>
    /// <remarks>
    ///     Possible error conditions: <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 Entire request failed: <c>            Error
    ///                 </c> is set, but not per-partition errors.
    ///             </description>
    ///         </item> <item>
    ///             <description>
    ///                 All partitions failed: <c>            Error
    ///                 </c> is set to the value of the last failed partition, but each partition may have
    ///                 different errors.
    ///             </description>
    ///         </item> <item>
    ///             <description>
    ///                 Some partitions failed: global <c>            Error
    ///                 </c> is success ( <see cref="Confluent.Kafka.ErrorCode.NoError" />).
    ///             </description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public class KafkaOffsetsCommittedEvent : IKafkaEvent
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaOffsetsCommittedEvent" /> class.
        /// </summary>
        /// <param name="committedOffsets">
        ///     The per-partition offsets and success or error information and the overall operation success or
        ///     error information.
        /// </param>
        public KafkaOffsetsCommittedEvent(CommittedOffsets? committedOffsets)
            : this(committedOffsets?.Offsets.ToList(), committedOffsets?.Error)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaOffsetsCommittedEvent" /> class.
        /// </summary>
        /// <param name="offsets">
        ///     The per-partition offsets and success or error information.
        /// </param>
        /// <param name="error">
        ///     The overall operation success or error information.
        /// </param>
        public KafkaOffsetsCommittedEvent(
            IReadOnlyCollection<TopicPartitionOffsetError>? offsets,
            Error? error = null)
        {
            Offsets = offsets ?? Array.Empty<TopicPartitionOffsetError>();
            Error = error ?? new Error(ErrorCode.NoError);
        }

        /// <summary>
        ///     Gets the per-partition offsets and success or error information.
        /// </summary>
        public IReadOnlyCollection<TopicPartitionOffsetError> Offsets { get; }

        /// <summary>
        ///     Gets the overall operation success or error information.
        /// </summary>
        public Error Error { get; }
    }
}
