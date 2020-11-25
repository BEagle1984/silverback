// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Confluent.Kafka;
using Silverback.Messaging.Broker;
using Silverback.Messaging.KafkaEvents.Statistics;

namespace Silverback.Messaging.KafkaEvents
{
    /// <summary>
    ///     Defines the handlers for the Kafka events such as partitions revoked/assigned, error, statistics and
    ///     offsets committed.
    /// </summary>
    public class KafkaConsumerEventsHandlers
    {
        /// <summary>
        ///     <para>
        ///         Gets or sets the handler to call when an error is reported by the consumer (e.g. connection
        ///         failures or all brokers down).
        ///     </para>
        ///     <para>
        ///         Note that the system (either the Kafka client itself or Silverback) will try to automatically
        ///         recover from all errors automatically, so these errors have to be considered purely informational.
        ///     </para>
        ///     <para>
        ///         The handler receives an <see cref="Error" /> containing the error details.
        ///     </para>
        ///     <para>
        ///         The result indicates whether the error was handled and doesn't need to be logged or handled in any
        ///         other way by Silverback.
        ///     </para>
        /// </summary>
        public Func<Error, KafkaConsumer, bool>? ErrorHandler { get; set; }

        /// <summary>
        ///     <para>
        ///         Gets or sets the handler that is called to report the result of offset commits.
        ///     </para>
        ///     <para>
        ///         The handler receives the per-partition offsets and success or error information and the overall
        ///         operation success or error information.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     Possible error conditions: <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 Entire request failed: <c>Error</c> is set, but not per-partition errors.
        ///             </description>
        ///         </item> <item>
        ///             <description>
        ///                 All partitions failed: <c>Error</c> is set to the value of the last failed partition, but
        ///                 each partition may have different errors.
        ///             </description>
        ///         </item> <item>
        ///             <description>
        ///                 Some partitions failed: global <c>Error</c> is success
        ///                 (<see cref="Confluent.Kafka.ErrorCode.NoError" />).
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        public Action<CommittedOffsets, KafkaConsumer>? OffsetsCommittedHandler { get; set; }

        /// <summary>
        ///     <para>
        ///         Gets or sets the handler that is called when a new consumer group partition assignment has been
        ///         received by the consumer.
        ///     </para>
        ///     <para>
        ///         Note: corresponding to every call to this handler there will be a corresponding call to the
        ///         partitions revoked handler (if one has been set using SetPartitionsRevokedHandler).
        ///     </para>
        ///     <para>
        ///         The handler receives a collection of <see cref="TopicPartitionOffset" /> representing the assigned
        ///         partitions.
        ///     </para>
        ///     <para>
        ///         The actual partitions to consume from and start offsets are specified by the return value of the
        ///         handler. This set of partitions is not required to match the assignment provided by the consumer
        ///         group, but typically will. Partition offsets may be a specific offset, or special value (
        ///         <c>Beginning</c>, <c>End</c> or <c>Unset</c>). If <c>Unset</c>, consumption will resume from the
        ///         last committed offset for each partition, or if there is no committed offset, in accordance with
        ///         the <c>auto.offset.reset</c> configuration property.
        ///     </para>
        /// </summary>
        public Func<IReadOnlyCollection<TopicPartition>, KafkaConsumer, IEnumerable<TopicPartitionOffset>>?
            PartitionsAssignedHandler { get; set; }

        /// <summary>
        ///     <para>
        ///         Gets or sets the handler that is called immediately prior to a group partition assignment being
        ///         revoked.
        ///     </para>
        ///     <para>
        ///         The handler receives a collection of <see cref="TopicPartitionOffset" /> representing the the set
        ///         of partitions the consumer is currently assigned to, and the current position of the consumer on
        ///         each of these partitions.
        ///     </para>
        /// </summary>
        public Action<IReadOnlyCollection<TopicPartitionOffset>, KafkaConsumer>? PartitionsRevokedHandler { get; set; }

        /// <summary>
        ///     <para>
        ///         Gets or sets the handler to call on statistics events.
        ///     </para>
        ///     <para>
        ///         You can enable statistics and set the statistics interval using the <c>StatisticsIntervalMs</c>
        ///         configuration property (disabled by default).
        ///     </para>
        /// </summary>
        public Action<KafkaStatistics, string, KafkaConsumer>? StatisticsHandler { get; set; }
    }
}
