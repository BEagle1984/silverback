// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Util;

namespace Silverback.Messaging;

/// <summary>
///     The Kafka topic and partition where the message must be produced to.
/// </summary>
public record KafkaProducerEndpoint : ProducerEndpoint<KafkaProducerEndpointConfiguration>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaProducerEndpoint" /> class.
    /// </summary>
    /// <param name="topic">
    ///     The topic.
    /// </param>
    /// <param name="partition">
    ///     The partition index.
    /// </param>
    /// <param name="configuration">
    ///     The producer configuration.
    /// </param>
    public KafkaProducerEndpoint(string topic, Partition partition, KafkaProducerEndpointConfiguration configuration)
        : this(new TopicPartition(topic, partition), configuration)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaProducerEndpoint" /> class.
    /// </summary>
    /// <param name="topicPartition">
    ///     The topic and partition.
    /// </param>
    /// <param name="configuration">
    ///     The producer configuration.
    /// </param>
    public KafkaProducerEndpoint(TopicPartition topicPartition, KafkaProducerEndpointConfiguration configuration)
        : base(Check.NotNull(topicPartition, nameof(topicPartition)).Topic, configuration)
    {
        TopicPartition = Check.NotNull(topicPartition, nameof(topicPartition));
    }

    /// <summary>
    ///     Gets the target topic and partition.
    /// </summary>
    public TopicPartition TopicPartition { get; }
}
