// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Confluent.Kafka;
using Silverback.Collections;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Builds the <see cref="KafkaConsumerConfiguration" />.
/// </summary>
public partial class KafkaConsumerConfigurationBuilder : KafkaClientConfigurationBuilder<ConsumerConfig, KafkaConsumerConfigurationBuilder>
{
    private readonly Dictionary<string, KafkaConsumerEndpointConfiguration> _endpoints = new();

    private bool? _commitOffsets;

    private int? _commitOffsetEach;

    private bool? _enableAutoRecovery;

    private bool? _processPartitionsIndependently;

    private int? _maxDegreeOfParallelism;

    private int? _backpressureLimit;

    /// <inheritdoc cref="KafkaClientConfigurationBuilder{TClientConfig,TBuilder}.This" />
    protected override KafkaConsumerConfigurationBuilder This => this;

    /// <summary>
    ///     Adds a consumer endpoint, which is a topic, a partition, or a group of topics/partitions that share the same configuration
    ///     (deserializer, error policies, etc.).
    /// </summary>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action" /> that takes the <see cref="KafkaConsumerConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder Consume(Action<KafkaConsumerEndpointConfigurationBuilder<object>> configurationBuilderAction) =>
        Consume<object>(configurationBuilderAction);

    /// <summary>
    ///     Adds a consumer endpoint, which is a topic, a partition, or a group of topics/partitions that share the same configuration
    ///     (deserializer, error policies, etc.).
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type (or base type) of the messages being consumed. This is used to setup the deserializer and will determine the type of the
    ///     message parameter in the nested configuration functions.
    /// </typeparam>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action" /> that takes the <see cref="KafkaConsumerConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder Consume<TMessage>(Action<KafkaConsumerEndpointConfigurationBuilder<TMessage>> configurationBuilderAction) =>
        Consume(null, configurationBuilderAction);

    /// <summary>
    ///     Adds a consumer endpoint, which is a topic, a partition, or a group of topics/partitions that share the same configuration
    ///     (deserializer, error policies, etc.).
    /// </summary>
    /// <param name="name">
    ///     The name is used to guarantee that a duplicated configuration is discarded and is also displayed in the logs.
    ///     By default the name will be generated concatenating the topic name(s).
    /// </param>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action" /> that takes the <see cref="KafkaConsumerConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder Consume(
        string? name,
        Action<KafkaConsumerEndpointConfigurationBuilder<object>> configurationBuilderAction) =>
        Consume<object>(name, configurationBuilderAction);

    /// <summary>
    ///     Adds a consumer endpoint, which is a topic, a partition, or a group of topics/partitions that share the same configuration
    ///     (deserializer, error policies, etc.).
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type (or base type) of the messages being consumed. This is used to setup the deserializer and will determine the type of the
    ///     message parameter in the nested configuration functions.
    /// </typeparam>
    /// <param name="name">
    ///     The name is used to guarantee that a duplicated configuration is discarded and is also displayed in the logs.
    ///     By default the name will be generated concatenating the topic name(s).
    /// </param>
    /// <param name="configurationBuilderAction">
    ///     An <see cref="Action" /> that takes the <see cref="KafkaConsumerConfigurationBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder Consume<TMessage>(
        string? name,
        Action<KafkaConsumerEndpointConfigurationBuilder<TMessage>> configurationBuilderAction)
    {
        Check.NullButNotEmpty(name, nameof(name));
        Check.NotNull(configurationBuilderAction, nameof(configurationBuilderAction));

        KafkaConsumerEndpointConfigurationBuilder<TMessage> builder = new(name);
        configurationBuilderAction.Invoke(builder);
        KafkaConsumerEndpointConfiguration endpointConfiguration = builder.Build();

        name ??= endpointConfiguration.RawName;

        if (_endpoints.ContainsKey(name))
            return this;

        _endpoints[name] = endpointConfiguration;

        return this;
    }

    /// <summary>
    ///     Client group id string. All clients sharing the same group.id belong to the same group.
    /// </summary>
    /// <param name="groupId">
    ///     Client group id string. All clients sharing the same group.id belong to the same group.
    /// </param>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder WithGroupId(string? groupId)
    {
        ClientConfig.GroupId = string.IsNullOrEmpty(groupId) ? KafkaConsumerConfiguration.UnsetGroupId : groupId;
        return This;
    }

    /// <summary>
    ///     Enable the offsets commit. This is the default.
    /// </summary>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder EnableOffsetsCommit()
    {
        _commitOffsets = true;
        return this;
    }

    /// <summary>
    ///     Disables the offsets commit.
    /// </summary>
    /// <remarks>
    ///     Disabling offsets commit will also disable auto commit (see <see cref="DisableAutoCommit" />) and clear the
    ///     <see cref="KafkaConsumerConfiguration.CommitOffsetEach" /> setting (see <see cref="CommitOffsetEach" />).
    /// </remarks>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder DisableOffsetsCommit()
    {
        _commitOffsets = false;
        _commitOffsetEach = null;
        DisableAutoCommit();
        return this;
    }

    /// <summary>
    ///     Automatically and periodically commit offsets in the background.
    /// </summary>
    /// <remarks>
    ///     Enabling automatic offsets commit will clear the <see cref="KafkaConsumerConfiguration.CommitOffsetEach" /> setting
    ///     (see <see cref="CommitOffsetEach" />).
    /// </remarks>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder EnableAutoCommit()
    {
        _commitOffsetEach = null;
        return WithEnableAutoCommit(true);
    }

    /// <summary>
    ///     Disable automatic offsets commit. Note: setting this does not prevent the consumer from fetching previously committed start
    ///     offsets. To circumvent this behaviour set specific start offsets per partition in the call to assign().
    /// </summary>
    /// <remarks>
    ///     See also <see cref="CommitOffsetEach" />.
    /// </remarks>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder DisableAutoCommit() => WithEnableAutoCommit(false);

    /// <summary>
    ///     Defines the number of message to be processed before committing the offset to the server. The most
    ///     reliable level is 1 but it reduces throughput.
    /// </summary>
    /// <remarks>
    ///     Setting this value automatically disables automatic offset commit (see <see cref="EnableAutoCommit" />/<see cref="DisableAutoCommit" />).
    /// </remarks>
    /// <param name="commitOffsetEach">
    ///     The number of message to be processed before committing the offset to the server.
    /// </param>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder CommitOffsetEach(int commitOffsetEach)
    {
        _commitOffsetEach = commitOffsetEach;
        WithEnableAutoCommit(false);
        return this;
    }

    /// <summary>
    ///     Specifies that the consumer has to be automatically recycled when a <see cref="KafkaException" />
    ///     is thrown while polling/consuming or an issues is detected (e.g. a poll timeout is reported). This is the default.
    /// </summary>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder EnableAutoRecovery()
    {
        _enableAutoRecovery = true;
        return this;
    }

    /// <summary>
    ///     Specifies that the consumer doesn't have to be automatically recycled when a <see cref="KafkaException" />
    ///     is thrown while polling/consuming or an issues is detected (e.g. a poll timeout is reported).
    /// </summary>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder DisableAutoRecovery()
    {
        _enableAutoRecovery = false;
        return this;
    }

    /// <summary>
    ///     Invoke the <see cref="IKafkaPartitionEofCallback" /> whenever a partition end of file is reached.
    /// </summary>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder EnablePartitionEof() => WithEnablePartitionEof(true);

    /// <summary>
    ///     Don't invoke the <see cref="IKafkaPartitionEofCallback" /> when a partition end of file is reached.
    /// </summary>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder DisablePartitionEof() => WithEnablePartitionEof(false);

    /// <summary>
    ///     Allow automatic topic creation on the broker when subscribing to or assigning non-existent topics. T
    ///     he broker must also be configured with `auto.create.topics.enable=true` for this configuration to take effect.
    ///     Note: The default value (false) is different from the Java consumer (true).
    ///     Requires broker version &gt;= 0.11.0.0, for older broker versions only the broker configuration applies.
    /// </summary>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder AllowAutoCreateTopics() => WithAllowAutoCreateTopics(true);

    /// <summary>
    ///     Disallow automatic topic creation on the broker when subscribing to or assigning non-existent topics.
    /// </summary>
    /// <returns>
    ///     The client configuration builder so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder DisallowAutoCreateTopics() => WithAllowAutoCreateTopics(false);

    /// <summary>
    ///     Specifies that the partitions must be processed independently. This means that a stream will published per each partition and
    ///     the sequences (<see cref="ChunkSequence" />, <see cref="BatchSequence" />, ...) cannot span across the partitions. This option
    ///     is enabled by default. Use <see cref="ProcessAllPartitionsTogether" /> to disable it.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder ProcessPartitionsIndependently()
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
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder ProcessAllPartitionsTogether()
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
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder LimitParallelism(int maxDegreeOfParallelism)
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
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder LimitBackpressure(int backpressureLimit)
    {
        _backpressureLimit = backpressureLimit;
        return this;
    }

    /// <summary>
    ///     Builds the <see cref="KafkaConsumerConfiguration" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfiguration" />.
    /// </returns>
    public KafkaConsumerConfiguration Build()
    {
        KafkaConsumerConfiguration configuration = new(ClientConfig.Clone());

        configuration = configuration with
        {
            CommitOffsets = _commitOffsets ?? configuration.CommitOffsets,
            CommitOffsetEach = _commitOffsetEach ?? configuration.CommitOffsetEach,
            EnableAutoRecovery = _enableAutoRecovery ?? configuration.EnableAutoRecovery,
            BackpressureLimit = _backpressureLimit ?? configuration.BackpressureLimit,
            ProcessPartitionsIndependently = _processPartitionsIndependently ?? configuration.ProcessPartitionsIndependently,
            Endpoints = _endpoints.Values.AsValueReadOnlyCollection()
        };

        // MaxDegreeOfParallelism is set in a second step because it might be influenced by ProcessPartitionsIndependently
        if (_maxDegreeOfParallelism.HasValue)
        {
            configuration = configuration with
            {
                MaxDegreeOfParallelism = _maxDegreeOfParallelism.Value
            };
        }

        configuration.Validate();

        return configuration;
    }
}
