// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using Silverback.Collections;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Builds the <see cref="KafkaConsumerConfiguration"/>.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the messages being consumed.
/// </typeparam>
public class KafkaConsumerConfigurationBuilder<TMessage>
    : ConsumerConfigurationBuilder<TMessage, KafkaConsumerConfiguration, KafkaConsumerConfigurationBuilder<TMessage>>
{
    private readonly KafkaClientConfiguration? _clientConfiguration;

    private readonly List<Action<KafkaClientConsumerConfiguration>> _clientConfigurationActions = new();

    private TopicPartitionOffset[]? _topicPartitionOffsets;

    private Func<IReadOnlyCollection<TopicPartition>, IEnumerable<TopicPartitionOffset>>? _partitionOffsetsProvider;

    private bool? _processPartitionsIndependently;

    private int? _maxDegreeOfParallelism;

    private int? _backpressureLimit;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaConsumerConfigurationBuilder{TMessage}" /> class.
    /// </summary>
    /// <param name="clientConfiguration">
    ///     The <see cref="KafkaClientConfiguration" /> to be used to initialize the <see cref="KafkaClientConsumerConfiguration" />.
    /// </param>
    /// <param name="endpointsConfigurationBuilder">
    ///     The optional <see cref="EndpointsConfigurationBuilder" /> that instantiated the builder.
    /// </param>
    public KafkaConsumerConfigurationBuilder(
        KafkaClientConfiguration? clientConfiguration = null,
        EndpointsConfigurationBuilder? endpointsConfigurationBuilder = null)
        : base(endpointsConfigurationBuilder)
    {
        _clientConfiguration = clientConfiguration;

        // Initialize default serializer according to TMessage type parameter
        DeserializeJson();
    }

    // TODO: Test
    /// <inheritdoc cref="EndpointConfigurationBuilder{TMessage,TConfiguration,TBuilder}.EndpointRawName" />
    public override string? EndpointRawName =>
        _topicPartitionOffsets == null ? null : string.Join(',', _topicPartitionOffsets.Select(topicPartitionOffset => topicPartitionOffset.Topic));

    /// <inheritdoc cref="EndpointConfigurationBuilder{TMessage,TEndpoint,TBuilder}.This" />
    protected override KafkaConsumerConfigurationBuilder<TMessage> This => this;

    /// <summary>
    ///     Specifies the topic to be subscribed.
    /// </summary>
    /// <param name="topic">
    ///     The topic.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder<TMessage> ConsumeFrom(string topic) =>
        ConsumeFrom(new[] { Check.NotEmpty(topic, nameof(topic)) });

    /// <summary>
    ///     Specifies the topics to be subscribed.
    /// </summary>
    /// <param name="topics">
    ///     The topics to be subscribed.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder<TMessage> ConsumeFrom(params string[] topics)
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
    ///     The <see cref="KafkaConsumerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder<TMessage> ConsumeFrom(string topic, params int[] partitions)
    {
        Check.NotEmpty(topic, nameof(topic));
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
    ///     The <see cref="KafkaConsumerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder<TMessage> ConsumeFrom(params TopicPartition[] topicPartitions)
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
    ///     The <see cref="KafkaConsumerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder<TMessage> ConsumeFrom(params TopicPartitionOffset[] topicPartitionOffsets)
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
    ///     The <see cref="KafkaConsumerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder<TMessage> ConsumeFrom(
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
    ///     The <see cref="KafkaConsumerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder<TMessage> ConsumeFrom(
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
    ///     The <see cref="KafkaConsumerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder<TMessage> ConsumeFrom(
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
    ///     The <see cref="KafkaConsumerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder<TMessage> ConsumeFrom(
        IEnumerable<string> topics,
        Func<IReadOnlyCollection<TopicPartition>, IEnumerable<TopicPartitionOffset>> partitionOffsetsProvider)
    {
        _topicPartitionOffsets = topics.Select(topic => new TopicPartitionOffset(topic, Partition.Any, Offset.Unset)).ToArray();
        _partitionOffsetsProvider = partitionOffsetsProvider;

        return this;
    }

    /// <summary>
    ///     Configures the Kafka client settings.
    /// </summary>
    /// <param name="clientConfigurationAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="KafkaClientConsumerConfiguration" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder<TMessage> ConfigureClient(Action<KafkaClientConsumerConfiguration> clientConfigurationAction)
    {
        Check.NotNull(clientConfigurationAction, nameof(clientConfigurationAction));

        _clientConfigurationActions.Add(clientConfigurationAction);

        return this;
    }

    /// <summary>
    ///     Specifies that the partitions must be processed independently. This means that a stream will published per each partition and
    ///     the sequences (<see cref="ChunkSequence" />, <see cref="BatchSequence" />, ...) cannot span across the partitions. This option
    ///     is enabled by default. Use <see cref="ProcessAllPartitionsTogether" /> to disable it.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder<TMessage> ProcessPartitionsIndependently()
    {
        _processPartitionsIndependently = true;
        return this;
    }

    /// <summary>
    ///     Specifies that all partitions must be processed together. This means that a single stream will published for the messages from
    ///     all the partitions and the sequences (<see cref="ChunkSequence" />, <see cref="BatchSequence" />, ...) can span across the
    ///     partitions.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder<TMessage> ProcessAllPartitionsTogether()
    {
        _processPartitionsIndependently = false;
        return this;
    }

    /// <summary>
    ///     Sets the maximum number of incoming message that can be processed concurrently. Up to a message per each subscribed partition
    ///     can be processed in parallel.
    ///     The default limit is 100.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">
    ///     The maximum number of incoming message that can be processed concurrently.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder<TMessage> LimitParallelism(int maxDegreeOfParallelism)
    {
        _maxDegreeOfParallelism = maxDegreeOfParallelism;
        return this;
    }

    /// <summary>
    ///     Sets the maximum number of messages to be consumed and enqueued waiting to be processed.
    ///     The limit will be applied per partition when processing the partitions independently (default).
    ///     The default limit is 2.
    /// </summary>
    /// <param name="backpressureLimit">
    ///     The maximum number of messages to be enqueued.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder<TMessage> LimitBackpressure(int backpressureLimit)
    {
        _backpressureLimit = backpressureLimit;
        return this;
    }

    /// <inheritdoc cref="EndpointConfigurationBuilder{TMessage,TEndpoint,TBuilder}.CreateConfiguration" />
    protected override KafkaConsumerConfiguration CreateConfiguration()
    {
        KafkaConsumerConfiguration configuration = new();

        configuration = configuration with
        {
            TopicPartitions = _topicPartitionOffsets?.AsValueReadOnlyCollection() ?? configuration.TopicPartitions,
            PartitionOffsetsProvider = _partitionOffsetsProvider,
            Client = GetClientConfiguration(),
            BackpressureLimit = _backpressureLimit ?? configuration.BackpressureLimit,
            ProcessPartitionsIndependently = _processPartitionsIndependently ?? configuration.ProcessPartitionsIndependently
        };

        // MaxDegreeOfParallelism is set in a second step because it might be influenced by ProcessPartitionsIndependently
        if (_maxDegreeOfParallelism.HasValue)
        {
            configuration = configuration with
            {
                MaxDegreeOfParallelism = _maxDegreeOfParallelism.Value
            };
        }

        return configuration;
    }

    private KafkaClientConsumerConfiguration GetClientConfiguration()
    {
        KafkaClientConsumerConfiguration config = new(_clientConfiguration);
        _clientConfigurationActions.ForEach(action => action.Invoke(config));
        return config.AsReadOnly();
    }
}
