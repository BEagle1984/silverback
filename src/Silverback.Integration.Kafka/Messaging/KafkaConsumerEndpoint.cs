// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;
using Silverback.Util;

namespace Silverback.Messaging;

/// <summary>
///     The Kafka topic and partition from which the message was consumed.
/// </summary>
public record KafkaConsumerEndpoint : ConsumerEndpoint<KafkaConsumerConfiguration>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaConsumerEndpoint" /> class.
    /// </summary>
    /// <param name="topic">
    ///     The topic.
    /// </param>
    /// <param name="partition">
    ///     The partition index.
    /// </param>
    /// <param name="configuration">
    ///     The consumer configuration.
    /// </param>
    public KafkaConsumerEndpoint(string topic, Partition partition, KafkaConsumerConfiguration configuration)
        : this(new TopicPartition(topic, partition), configuration)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaConsumerEndpoint" /> class.
    /// </summary>
    /// <param name="topicPartition">
    ///     The topic and partition.
    /// </param>
    /// <param name="configuration">
    ///     The consumer configuration.
    /// </param>
    public KafkaConsumerEndpoint(TopicPartition topicPartition, KafkaConsumerConfiguration configuration)
        : base(Check.NotNull(topicPartition, nameof(topicPartition)).Topic, configuration)
    {
        TopicPartition = topicPartition;
    }

    /// <summary>
    ///     Gets the source topic and partition.
    /// </summary>
    public TopicPartition TopicPartition { get; }
}
