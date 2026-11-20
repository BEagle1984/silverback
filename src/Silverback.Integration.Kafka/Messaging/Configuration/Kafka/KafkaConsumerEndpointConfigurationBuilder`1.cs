// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Silverback.Collections;
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
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" />.
    /// </param>
    /// <param name="friendlyName">
    ///     An optional friendly to be shown in the human-targeted output (e.g. logs, health checks result, etc.).
    /// </param>
    public KafkaConsumerEndpointConfigurationBuilder(IServiceProvider serviceProvider, string? friendlyName = null)
        : base(serviceProvider, friendlyName)
    {
    }

    /// <inheritdoc cref="EndpointConfigurationBuilder{TMessage,TConfiguration,TBuilder}.This" />
    protected override KafkaConsumerEndpointConfigurationBuilder<TMessage> This => this;

    /// <summary>
    ///     Specifies the topic to be subscribed.
    /// </summary>
    /// <param name="topic">
    ///     The topic, or a regular expression starting with <c>^</c> to subscribe all matching topics (e.g. <c>^orders-.*</c>).
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    [SuppressMessage("Minor Code Smell", "S3878:Arrays should not be created for params parameters", Justification = "Passing the topic directly would resolve to this same overload and recurse, so the collection expression targets the params string[] overload")]
    [SuppressMessage("Minor Code Smell", "S3220:Method calls should not resolve ambiguously to overloads with \"params\"", Justification = "Passing the topic directly would resolve to this same overload and recurse, so the collection expression targets the params string[] overload")]
    public KafkaConsumerEndpointConfigurationBuilder<TMessage> ConsumeFrom(string topic) =>
        ConsumeFrom([Check.NotNullOrEmpty(topic)]);

    /// <summary>
    ///     Specifies the topics to be subscribed.
    /// </summary>
    /// <param name="topics">
    ///     The topics to be subscribed. A topic starting with <c>^</c> is a regular expression subscribing all matching topics (e.g.
    ///     <c>^orders-.*</c>).
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerEndpointConfigurationBuilder<TMessage> ConsumeFrom(params string[] topics)
    {
        Check.HasNoEmpties(topics);

        _topicPartitionOffsets = [.. topics.Select(topic => new TopicPartitionOffset(topic, Partition.Any, Offset.Unset))];
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
        Check.NotNullOrEmpty(topic);
        Check.NotEmpty(partitions);

        _topicPartitionOffsets = [.. partitions.Select(partition => new TopicPartitionOffset(topic, partition, Offset.Unset))];
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
        Check.HasNoNulls(topicPartitions);

        _topicPartitionOffsets = [.. topicPartitions.Select(topicPartition => new TopicPartitionOffset(topicPartition, Offset.Unset))];
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
        Check.HasNoNulls(topicPartitionOffsets);

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
        ConsumeFrom([topic], partitionsProvider);

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
        ConsumeFrom([topic], partitionOffsetsProvider);

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
            topicPartitions => ValueTask.FromResult(partitionOffsetsProvider.Invoke(topicPartitions)));

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
        _topicPartitionOffsets = [.. topics.Select(topic => new TopicPartitionOffset(topic, Partition.Any, Offset.Unset))];
        _partitionOffsetsProvider = partitionOffsetsProvider;

        return this;
    }

    /// <summary>
    ///     Specifies the topic whose partitions are all to be consumed from the beginning. The partitions are retrieved from the topic
    ///     metadata and statically assigned, regardless of the offsets committed to Kafka (a client side offset store still applies).
    ///     The topic patterns aren't supported.
    /// </summary>
    /// <param name="topic">
    ///     The topic.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerEndpointConfigurationBuilder<TMessage> ConsumeFromBeginning(string topic) =>
        ConsumeFromBeginning([Check.NotNullOrEmpty(topic)], topicPartitions => topicPartitions);

    /// <summary>
    ///     Specifies the topics whose partitions are all to be consumed from the beginning. The partitions are retrieved from the topic
    ///     metadata and statically assigned, regardless of the offsets committed to Kafka (a client side offset store still applies).
    ///     The topic patterns aren't supported.
    /// </summary>
    /// <param name="topics">
    ///     The topics.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerEndpointConfigurationBuilder<TMessage> ConsumeFromBeginning(params string[] topics) =>
        ConsumeFromBeginning(topics, topicPartitions => topicPartitions);

    /// <summary>
    ///     Specifies the topic partitions to be consumed from the beginning.
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
    public KafkaConsumerEndpointConfigurationBuilder<TMessage> ConsumeFromBeginning(string topic, params int[] partitions)
    {
        Check.NotNullOrEmpty(topic);
        Check.NotEmpty(partitions);

        if (partitions.Any(partition => partition < 0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(partitions),
                "The partitions to be assigned statically must be specified explicitly.");
        }

        TopicPartitionOffset[] topicPartitionOffsets =
            [.. partitions.Select(partition => new TopicPartitionOffset(topic, partition, Offset.Beginning))];

        return ConsumeFrom(topicPartitionOffsets);
    }

    /// <summary>
    ///     Specifies the topics and partitions to be consumed from the beginning.
    /// </summary>
    /// <param name="topicPartitions">
    ///     The topic partitions to be consumed.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerEndpointConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerEndpointConfigurationBuilder<TMessage> ConsumeFromBeginning(params TopicPartition[] topicPartitions)
    {
        Check.HasNoNulls(topicPartitions);

        if (topicPartitions.Any(topicPartition => topicPartition.Partition == Partition.Any))
        {
            throw new ArgumentOutOfRangeException(
                nameof(topicPartitions),
                "The partitions to be assigned statically must be specified explicitly.");
        }

        TopicPartitionOffset[] topicPartitionOffsets =
            [.. topicPartitions.Select(topicPartition => new TopicPartitionOffset(topicPartition, Offset.Beginning))];

        return ConsumeFrom(topicPartitionOffsets);
    }

    /// <summary>
    ///     Specifies the topic and a function that returns the partitions to be consumed from the beginning.
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
    public KafkaConsumerEndpointConfigurationBuilder<TMessage> ConsumeFromBeginning(
        string topic,
        Func<IReadOnlyCollection<TopicPartition>, IEnumerable<TopicPartition>> partitionsProvider) =>
        ConsumeFromBeginning([Check.NotNullOrEmpty(topic)], partitionsProvider);

    /// <summary>
    ///     Specifies the topics and a function that returns the partitions to be consumed from the beginning.
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
    public KafkaConsumerEndpointConfigurationBuilder<TMessage> ConsumeFromBeginning(
        string[] topics,
        Func<IReadOnlyCollection<TopicPartition>, IEnumerable<TopicPartition>> partitionsProvider)
    {
        Check.HasNoEmpties(topics);
        Check.NotNull(partitionsProvider);

        return ConsumeFrom(
            topics,
            topicPartitions =>
                partitionsProvider.Invoke(topicPartitions)
                    .Select(topicPartition => new TopicPartitionOffset(topicPartition, Offset.Beginning)));
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
