// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Silverback.Collections;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Builds the <see cref="KafkaConsumerConfiguration" />.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages being consumed.
/// </typeparam>
public class KafkaConsumerEndpointConfigurationBuilder<TMessage>
    : ConsumerEndpointConfigurationBuilder<TMessage, KafkaConsumerEndpointConfiguration, KafkaConsumerEndpointConfigurationBuilder<TMessage>>
{
    private TopicPartitionOffset[]? _topicPartitionOffsets;

    private Func<IReadOnlyCollection<TopicPartition>, ValueTask<IEnumerable<TopicPartitionOffset>>>? _partitionOffsetsProvider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaConsumerEndpointConfigurationBuilder{TMessage}" /> class.
    /// </summary>
    /// <param name="friendlyName">
    ///     An optional friendly to be shown in the human-targeted output (e.g. logs, health checks result, etc.).
    /// </param>
    public KafkaConsumerEndpointConfigurationBuilder(string? friendlyName = null)
        : base(friendlyName)
    {
    }

    /// <inheritdoc cref="EndpointConfigurationBuilder{TMessage,TConfiguration,TBuilder}.EndpointRawName" />
    // TODO: Test
    public override string? EndpointRawName => _topicPartitionOffsets == null
        ? null
        : string.Join(',', _topicPartitionOffsets.Select(topicPartitionOffset => topicPartitionOffset.Topic));

    /// <inheritdoc cref="EndpointConfigurationBuilder{TMessage,TConfiguration,TBuilder}.This" />
    protected override KafkaConsumerEndpointConfigurationBuilder<TMessage> This => this;

    /// <summary>
    ///     Specifies the topic to be subscribed.
    /// </summary>
    /// <param name="topic">
    ///     The topic.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerEndpointConfigurationBuilder<TMessage> ConsumeFrom(string topic) =>
        ConsumeFrom(new[] { Check.NotNullOrEmpty(topic, nameof(topic)) });

    /// <summary>
    ///     Specifies the topics to be subscribed.
    /// </summary>
    /// <param name="topics">
    ///     The topics to be subscribed.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerEndpointConfigurationBuilder<TMessage> ConsumeFrom(params string[] topics)
    {
        Check.HasNoEmpties(topics, nameof(topics));

        _topicPartitionOffsets = topics.Select(topic => new TopicPartitionOffset(topic, Partition.Any, Offset.Unset)).ToArray();
        _partitionOffsetsProvider = null;

        return this;
    }

    /// <summary>
    ///     Specifies the topic partitions to be consumed.
    /// </summary>
    /// <param name="topic">
    ///     The topic.
    /// </param>
    /// <param name="partitions">
    ///     The indexes of the partitions to be consumed.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerEndpointConfigurationBuilder<TMessage> ConsumeFrom(string topic, params int[] partitions)
    {
        Check.NotNullOrEmpty(topic, nameof(topic));
        Check.NotEmpty(partitions, nameof(partitions));

        _topicPartitionOffsets = partitions.Select(partition => new TopicPartitionOffset(topic, partition, Offset.Unset)).ToArray();
        _partitionOffsetsProvider = null;

        return this;
    }

    /// <summary>
    ///     Specifies the topics and partitions to be consumed.
    /// </summary>
    /// <param name="topicPartitions">
    ///     The topic partitions to be consumed.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerEndpointConfigurationBuilder<TMessage> ConsumeFrom(params TopicPartition[] topicPartitions)
    {
        Check.HasNoNulls(topicPartitions, nameof(topicPartitions));

        _topicPartitionOffsets = topicPartitions.Select(topicPartition => new TopicPartitionOffset(topicPartition, Offset.Unset)).ToArray();
        _partitionOffsetsProvider = null;

        return this;
    }

    /// <summary>
    ///     Specifies the topics and partitions to be consumed, as well as the starting offsets.
    /// </summary>
    /// <param name="topicPartitionOffsets">
    ///     The topic partitions to be consumed and their starting offset.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerEndpointConfigurationBuilder<TMessage> ConsumeFrom(params TopicPartitionOffset[] topicPartitionOffsets)
    {
        Check.HasNoNulls(topicPartitionOffsets, nameof(topicPartitionOffsets));

        _topicPartitionOffsets = topicPartitionOffsets;
        _partitionOffsetsProvider = null;

        return this;
    }

    /// <summary>
    ///     Specifies the topic and a function that returns the partitions to be consumed.
    /// </summary>
    /// <param name="topic">
    ///     The topic.
    /// </param>
    /// <param name="partitionsProvider">
    ///     A function that receives all available <see cref="TopicPartition" /> for the topic and returns the collection of
    ///     <see cref="TopicPartition" /> to be consumed.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerEndpointConfigurationBuilder<TMessage> ConsumeFrom(
        string topic,
        Func<IReadOnlyCollection<TopicPartition>, IEnumerable<TopicPartition>> partitionsProvider) =>
        ConsumeFrom(new[] { topic }, partitionsProvider);

    /// <summary>
    ///     Specifies the topic and a function that returns the partitions to be consumed, as well as the starting offsets.
    /// </summary>
    /// <param name="topic">
    ///     The topic.
    /// </param>
    /// <param name="partitionOffsetsProvider">
    ///     A function that receives all available <see cref="TopicPartition" /> for the topic and returns the collection of
    ///     <see cref="TopicPartitionOffset" /> containing the partitions to be consumed and their starting offsets.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerEndpointConfigurationBuilder<TMessage> ConsumeFrom(
        string topic,
        Func<IReadOnlyCollection<TopicPartition>, IEnumerable<TopicPartitionOffset>> partitionOffsetsProvider) =>
        ConsumeFrom(new[] { topic }, partitionOffsetsProvider);

    /// <summary>
    ///     Specifies the topics and a function that returns the partitions to be consumed, as well as the starting offsets.
    /// </summary>
    /// <param name="topics">
    ///     The topics.
    /// </param>
    /// <param name="partitionsProvider">
    ///     A function that receives all available <see cref="TopicPartition" /> for the topics and returns the collection of
    ///     <see cref="TopicPartition" /> to be consumed.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerEndpointConfigurationBuilder<TMessage> ConsumeFrom(
        string[] topics,
        Func<IReadOnlyCollection<TopicPartition>, IEnumerable<TopicPartition>> partitionsProvider) =>
        ConsumeFrom(
            topics,
            topicPartitions =>
                partitionsProvider.Invoke(topicPartitions)
                    .Select(topicPartition => new TopicPartitionOffset(topicPartition, Offset.Unset)));

    /// <summary>
    ///     Specifies the topics and a function that returns the partitions to be consumed, as well as the starting offsets.
    /// </summary>
    /// <param name="topics">
    ///     The topics.
    /// </param>
    /// <param name="partitionOffsetsProvider">
    ///     A function that receives all available <see cref="TopicPartition" /> for the topics and returns the collection of
    ///     <see cref="TopicPartitionOffset" /> containing the partitions to be consumed and their starting offsets.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerEndpointConfigurationBuilder<TMessage> ConsumeFrom(
        IEnumerable<string> topics,
        Func<IReadOnlyCollection<TopicPartition>, IEnumerable<TopicPartitionOffset>> partitionOffsetsProvider) =>
        ConsumeFrom(
            topics,
            topicPartitions => ValueTaskFactory.FromResult(partitionOffsetsProvider.Invoke(topicPartitions)));

    /// <summary>
    ///     Specifies the topics and a function that returns the partitions to be consumed, as well as the starting offsets.
    /// </summary>
    /// <param name="topics">
    ///     The topics.
    /// </param>
    /// <param name="partitionOffsetsProvider">
    ///     A function that receives all available <see cref="TopicPartition" /> for the topics and returns the collection of
    ///     <see cref="TopicPartitionOffset" /> containing the partitions to be consumed and their starting offsets.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerEndpointConfigurationBuilder<TMessage> ConsumeFrom(
        IEnumerable<string> topics,
        Func<IReadOnlyCollection<TopicPartition>, ValueTask<IEnumerable<TopicPartitionOffset>>> partitionOffsetsProvider)
    {
        _topicPartitionOffsets = topics.Select(topic => new TopicPartitionOffset(topic, Partition.Any, Offset.Unset)).ToArray();
        _partitionOffsetsProvider = partitionOffsetsProvider;

        return this;
    }

    /// <inheritdoc cref="EndpointConfigurationBuilder{TMessage,TConfiguration,TBuilder}.CreateConfiguration" />
    protected override KafkaConsumerEndpointConfiguration CreateConfiguration()
    {
        KafkaConsumerEndpointConfiguration configuration = new();

        configuration = configuration with
        {
            TopicPartitions = _topicPartitionOffsets?.AsValueReadOnlyCollection() ?? configuration.TopicPartitions,
            PartitionOffsetsProvider = _partitionOffsetsProvider
        };

        return configuration;
    }
}
