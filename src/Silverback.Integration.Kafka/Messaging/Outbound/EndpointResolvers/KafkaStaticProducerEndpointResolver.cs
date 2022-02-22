// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Outbound.EndpointResolvers;

/// <summary>
///     Statically resolves to the same target topic and partition (if specified) for every message being produced.
/// </summary>
public sealed class KafkaStaticProducerEndpointResolver : StaticProducerEndpointResolver<KafkaProducerEndpoint, KafkaProducerConfiguration>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaStaticProducerEndpointResolver" /> class.
    /// </summary>
    /// <param name="topic">
    ///     The target topic.
    /// </param>
    /// <param name="partition">
    ///     The optional target partition index.
    /// </param>
    public KafkaStaticProducerEndpointResolver(string topic, int? partition = null)
        : this(new TopicPartition(Check.NotNullOrEmpty(topic, nameof(topic)), partition ?? Partition.Any))
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaStaticProducerEndpointResolver" /> class.
    /// </summary>
    /// <param name="topicPartition">
    ///     The target topic and partition.
    /// </param>
    public KafkaStaticProducerEndpointResolver(TopicPartition topicPartition)
        : base(Check.NotNull(topicPartition, nameof(topicPartition)).ToDisplayString())
    {
        if (topicPartition.Partition < Partition.Any)
            throw new ArgumentException("The partition must be a value greater or equal to 0, or Partition.Any.", nameof(topicPartition));

        TopicPartition = topicPartition;
    }

    /// <summary>
    ///     Gets the target topic and partition.
    /// </summary>
    public TopicPartition TopicPartition { get; }

    /// <inheritdoc cref="StaticProducerEndpointResolver{TEndpoint,TConfiguration}.GetEndpointCore" />
    protected override KafkaProducerEndpoint GetEndpointCore(KafkaProducerConfiguration configuration) => new(TopicPartition, configuration);
}
