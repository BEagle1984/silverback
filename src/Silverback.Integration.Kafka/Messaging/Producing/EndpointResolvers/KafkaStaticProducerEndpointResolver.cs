// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Producing.EndpointResolvers;

/// <summary>
///     Statically resolves to the same target topic and partition (if specified) for every message being produced.
/// </summary>
public sealed class KafkaStaticProducerEndpointResolver
    : StaticProducerEndpointResolver<KafkaProducerEndpoint, KafkaProducerEndpointConfiguration>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaStaticProducerEndpointResolver" /> class.
    /// </summary>
    /// <param name="topic">
    ///     The target topic. Must not be empty.
    /// </param>
    /// <param name="partition">
    ///     The optional target partition index. If specified, it must be greater than or equal to 0, or <see cref="Partition.Any" />.
    /// </param>
    public KafkaStaticProducerEndpointResolver(string topic, int? partition = null)
        : this(new TopicPartition(Check.NotNullOrEmpty(topic), partition ?? Partition.Any))
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaStaticProducerEndpointResolver" /> class.
    /// </summary>
    /// <param name="topicPartition">
    ///     The target topic and partition. The <see cref="Confluent.Kafka.TopicPartition.Partition" /> must be greater than or equal to 0,
    ///     or <see cref="Partition.Any" />.
    /// </param>
    public KafkaStaticProducerEndpointResolver(TopicPartition topicPartition)
        : base(Check.NotNull(topicPartition).ToDisplayString())
    {
        if (topicPartition.Partition < Partition.Any)
            throw new ArgumentException("The partition must be greater than or equal to 0, or Partition.Any.", nameof(topicPartition));

        TopicPartition = topicPartition;
    }

    /// <summary>
    ///     Gets the target topic and partition.
    /// </summary>
    public TopicPartition TopicPartition { get; }

    /// <inheritdoc cref="StaticProducerEndpointResolver{TEndpoint,TConfiguration}.GetEndpointCore" />
    protected override KafkaProducerEndpoint GetEndpointCore(KafkaProducerEndpointConfiguration configuration) =>
        new(TopicPartition, configuration);
}
