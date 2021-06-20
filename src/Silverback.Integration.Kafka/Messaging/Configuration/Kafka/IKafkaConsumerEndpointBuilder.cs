// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Confluent.Kafka;
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
        ///     Specifies the name of the topic to be consumed and a function that returns the partitions to be assigned.
        /// </summary>
        /// <param name="topicName">
        ///     The name of the topic.
        /// </param>
        /// <param name="topicPartitionsResolver">
        ///     A function that receives all available <see cref="TopicPartition" /> for the topic and returns the
        ///     collection of <see cref="TopicPartition" /> to be assigned.
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaConsumerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IKafkaConsumerEndpointBuilder ConsumeFrom(
            string topicName,
            Func<IReadOnlyCollection<TopicPartition>, IEnumerable<TopicPartition>> topicPartitionsResolver);

        /// <summary>
        ///     Specifies the name of the topic to be consumed and a function that returns the partitions to be assigned.
        /// </summary>
        /// <param name="topicName">
        ///     The name of the topic.
        /// </param>
        /// <param name="topicPartitionsResolver">
        ///     A function that receives all available <see cref="TopicPartition" /> for the topic and returns the
        ///     collection of <see cref="TopicPartitionOffset" /> to be assigned.
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaConsumerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IKafkaConsumerEndpointBuilder ConsumeFrom(
            string topicName,
            Func<IReadOnlyCollection<TopicPartition>, IEnumerable<TopicPartitionOffset>>
                topicPartitionsResolver);

        /// <summary>
        ///     Specifies the name of the topic to be consumed and a function that returns the partitions to be assigned.
        /// </summary>
        /// <param name="topicNames">
        ///     The name of the topics.
        /// </param>
        /// <param name="topicPartitionsResolver">
        ///     A function that receives all available <see cref="TopicPartition" /> for the topics and returns the
        ///     collection of <see cref="TopicPartition" /> to be assigned.
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaConsumerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IKafkaConsumerEndpointBuilder ConsumeFrom(
            string[] topicNames,
            Func<IReadOnlyCollection<TopicPartition>, IEnumerable<TopicPartition>> topicPartitionsResolver);

        /// <summary>
        ///     Specifies the name of the topic to be consumed and a function that returns the partitions to be assigned.
        /// </summary>
        /// <param name="topicNames">
        ///     The name of the topics.
        /// </param>
        /// <param name="topicPartitionsResolver">
        ///     A function that receives all available <see cref="TopicPartition" /> for the topics and returns the
        ///     collection of <see cref="TopicPartitionOffset" /> to be assigned.
        /// </param>
        /// <returns>
        ///     The <see cref="IKafkaConsumerEndpointBuilder" /> so that additional calls can be chained.
        /// </returns>
        IKafkaConsumerEndpointBuilder ConsumeFrom(
            string[] topicNames,
            Func<IReadOnlyCollection<TopicPartition>, IEnumerable<TopicPartitionOffset>>
                topicPartitionsResolver);

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
    }
}
