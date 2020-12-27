// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Confluent.Kafka;
using Silverback.Messaging.Broker;
using Silverback.Messaging.KafkaEvents.Statistics;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Sequences.Chunking;

namespace Silverback.Messaging.Configuration.Kafka
{
    /// <summary>
    ///     Builds the <see cref="KafkaConsumerEndpoint" />.
    /// </summary>
    public interface IKafkaConsumerEndpointBuilder : IConsumerEndpointBuilder<IKafkaConsumerEndpointBuilder>
    {
        /// <summary>
        ///     Specifies the name of the topics to be consumed.
        /// </summary>
        /// <param name="topicNames">
        ///     The name of the topics.
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaConsumerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IKafkaConsumerEndpointBuilder ConsumeFrom(params string[] topicNames);

        /// <summary>
        ///     Specifies the topics and partitions to be consumed.
        /// </summary>
        /// <param name="topicPartitions">
        ///     The topics and partitions to be consumed.
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaConsumerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IKafkaConsumerEndpointBuilder ConsumeFrom(params TopicPartition[] topicPartitions);

        /// <summary>
        ///     Specifies the topics and partitions to be consumed.
        /// </summary>
        /// <param name="topicPartitions">
        ///     The topics and partitions to be consumed and the starting offset.
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaConsumerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IKafkaConsumerEndpointBuilder ConsumeFrom(params TopicPartitionOffset[] topicPartitions);

        /// <summary>
        ///     Configures the Kafka client properties.
        /// </summary>
        /// <param name="configAction">
        ///     An <see cref="Action{T}" /> that takes the <see cref="IKafkaConsumerEndpointBuilder" /> and configures
        ///     it.
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaConsumerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IKafkaConsumerEndpointBuilder Configure(Action<KafkaConsumerConfig> configAction);

        /// <summary>
        ///     Specifies that the partitions must be processed independently. This means that a stream will published
        ///     per each partition and the sequences (<see cref="ChunkSequence" />, <see cref="BatchSequence" />, ...)
        ///     cannot span across the partitions. This option is enabled by default. Use
        ///     <see cref="ProcessAllPartitionsTogether" /> to disable it.
        /// </summary>
        /// <returns>
        ///     The <see cref="IKafkaConsumerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IKafkaConsumerEndpointBuilder ProcessPartitionsIndependently();

        /// <summary>
        ///     Specifies that all partitions must be processed together. This means that a single stream will
        ///     published for the messages from all the partitions and the sequences (<see cref="ChunkSequence" />,
        ///     <see cref="BatchSequence" />, ...) can span across the partitions.
        /// </summary>
        /// <returns>
        ///     The <see cref="IKafkaConsumerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IKafkaConsumerEndpointBuilder ProcessAllPartitionsTogether();

        /// <summary>
        ///     Sets the maximum number of incoming message that can be processed concurrently. Up to a message per
        ///     each subscribed partition can be processed in parallel.
        ///     The default limit is 10.
        /// </summary>
        /// <param name="maxDegreeOfParallelism">
        ///     The maximum number of incoming message that can be processed concurrently.
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaConsumerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IKafkaConsumerEndpointBuilder LimitParallelism(int maxDegreeOfParallelism);

        /// <summary>
        ///     Sets the maximum number of messages to be consumed and enqueued waiting to be processed.
        ///     The limit will be applied per partition when processing the partitions independently (default).
        ///     The default limit is 1.
        /// </summary>
        /// <param name="backpressureLimit">
        ///     The maximum number of messages to be enqueued.
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaConsumerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IKafkaConsumerEndpointBuilder LimitBackpressure(int backpressureLimit);

        /// <summary>
        ///     <para>
        ///         Sets the handler to call when an error is reported by the consumer (e.g. connection
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
        /// <param name="handler">
        ///     The event handler.
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaConsumerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IKafkaConsumerEndpointBuilder OnKafkaError(Func<Error, KafkaConsumer, bool> handler);

        /// <summary>
        ///     <para>
        ///         Sets the handler that is called to report the result of offset commits.
        ///     </para>
        ///     <para>
        ///         The handler receives the per-partition offsets and success or error information and the overall
        ///         operation success or error information.
        ///     </para>
        /// </summary>
        /// <param name="handler">
        ///     The event handler.
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaConsumerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
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
        IKafkaConsumerEndpointBuilder OnOffsetsCommitted(Action<CommittedOffsets, KafkaConsumer> handler);

        /// <summary>
        ///     <para>
        ///         Sets the handler that is called when a new consumer group partition assignment has been
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
        /// <param name="handler">
        ///     The event handler.
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaConsumerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IKafkaConsumerEndpointBuilder OnPartitionsAssigned(
            Func<IReadOnlyCollection<TopicPartition>, KafkaConsumer, IEnumerable<TopicPartitionOffset>> handler);

        /// <summary>
        ///     <para>
        ///         Sets the handler that is called immediately prior to a group partition assignment being
        ///         revoked.
        ///     </para>
        ///     <para>
        ///         The handler receives a collection of <see cref="TopicPartitionOffset" /> representing the the set
        ///         of partitions the consumer is currently assigned to, and the current position of the consumer on
        ///         each of these partitions.
        ///     </para>
        /// </summary>
        /// <param name="handler">
        ///     The event handler.
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaConsumerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IKafkaConsumerEndpointBuilder OnPartitionsRevoked(
            Action<IReadOnlyCollection<TopicPartitionOffset>, KafkaConsumer> handler);

        /// <summary>
        ///     <para>
        ///         Sets the handler to call on statistics events.
        ///     </para>
        ///     <para>
        ///         You can enable statistics and set the statistics interval using the <c>StatisticsIntervalMs</c>
        ///         configuration property (disabled by default).
        ///     </para>
        /// </summary>
        /// <param name="handler">
        ///     The event handler.
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaConsumerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IKafkaConsumerEndpointBuilder OnStatisticsReceived(Action<KafkaStatistics, string, KafkaConsumer> handler);
    }
}
