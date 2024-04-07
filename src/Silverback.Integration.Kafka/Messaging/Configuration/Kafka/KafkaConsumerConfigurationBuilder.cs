// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Confluent.Kafka;
using Silverback.Collections;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Builds the <see cref="KafkaConsumerConfiguration" />.
/// </summary>
public partial class KafkaConsumerConfigurationBuilder : KafkaClientConfigurationBuilder<ConsumerConfig, KafkaConsumerConfigurationBuilder>
{
    private readonly Dictionary<string, KafkaConsumerEndpointConfiguration> _endpoints = [];

    private bool? _commitOffsets;

    private int? _commitOffsetEach;

    private KafkaOffsetStoreSettings? _clientSideOffsetStoreSettings;

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
    ///     Sets the client group id string. All clients sharing the same group.id belong to the same group.
    /// </summary>
    /// <param name="groupId">
    ///     The client group id string. All clients sharing the same group.id belong to the same group.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
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
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
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
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
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
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
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
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
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
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder CommitOffsetEach(int commitOffsetEach)
    {
        _commitOffsetEach = commitOffsetEach;
        WithEnableAutoCommit(false);
        return this;
    }

    /// <summary>
    ///     Specifies that the offsets have to stored in the specified client store, additionally to or instead of being committed to the
    ///     message broker.
    /// </summary>
    /// <param name="settingsBuilderFunc">
    ///     A <see cref="Func{T}" /> that takes the <see cref="KafkaOffsetStoreSettingsBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder StoreOffsetsClientSide(Func<KafkaOffsetStoreSettingsBuilder, IKafkaOffsetStoreSettingsImplementationBuilder> settingsBuilderFunc)
    {
        Check.NotNull(settingsBuilderFunc, nameof(settingsBuilderFunc));

        return StoreOffsetsClientSide(settingsBuilderFunc.Invoke(new KafkaOffsetStoreSettingsBuilder()).Build());
    }

    /// <summary>
    ///     Specifies that the offsets have to stored in the specified client store, additionally to or instead of being committed to the
    ///     message broker.
    /// </summary>
    /// <param name="settings">
    ///     The offset store settings.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder StoreOffsetsClientSide(KafkaOffsetStoreSettings settings)
    {
        Check.NotNull(settings, nameof(settings));
        settings.Validate();

        _clientSideOffsetStoreSettings = settings;
        return This;
    }

    /// <summary>
    ///     Specifies that the consumer has to be automatically recycled when a <see cref="KafkaException" />
    ///     is thrown while polling/consuming or an issues is detected (e.g. a poll timeout is reported). This is the default.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
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
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
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
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder EnablePartitionEof() => WithEnablePartitionEof(true);

    /// <summary>
    ///     Don't invoke the <see cref="IKafkaPartitionEofCallback" /> when a partition end of file is reached.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder DisablePartitionEof() => WithEnablePartitionEof(false);

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
    ///     Sets a comma-separated list of fields that may be optionally set in <see cref="ConsumeResult{TKey,TValue}" /> objects returned by
    ///     the <see cref="Consumer{TKey,TValue}.Consume(System.TimeSpan)" /> method. Disabling fields that you do not require will improve
    ///     throughput and reduce memory consumption. Allowed values: <c>headers</c>, <c>timestamp</c>, <c>topic</c>, <c>all</c>, <c>none</c>.
    /// </summary>
    /// <param name="consumeResultFields">
    ///     A comma-separated list of fields that may be optionally set in <see cref="ConsumeResult{TKey,TValue}" />.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaConsumerConfigurationBuilder WithConsumeResultFields(string? consumeResultFields);

    /// <summary>
    ///     Sets the action to take when there is no initial offset in the offset store or the desired offset is out of range:
    ///     <see cref="Confluent.Kafka.AutoOffsetReset.Earliest" /> to automatically reset to the smallest offset,
    ///     <see cref="Confluent.Kafka.AutoOffsetReset.Latest" /> to automatically reset to the largest offset, and
    ///     <see cref="Confluent.Kafka.AutoOffsetReset.Error" /> to trigger an error (ERR__AUTO_OFFSET_RESET).
    /// </summary>
    /// <param name="autoOffsetReset">
    ///     The action to take when there is no initial offset in the offset store or the desired offset is out of range.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaConsumerConfigurationBuilder WithAutoOffsetReset(AutoOffsetReset? autoOffsetReset);

    /// <summary>
    ///     Specifies that the offset needs to be reset to the smallest offset, when there is no initial offset in the offset store or the desired
    ///     offset is out of range.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder AutoResetOffsetToEarliest() => WithAutoOffsetReset(AutoOffsetReset.Earliest);

    /// <summary>
    ///     Specifies that the offset needs to be reset to the largest offset, when there is no initial offset in the offset store or the desired
    ///     offset is out of range.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder AutoResetOffsetToLatest() => WithAutoOffsetReset(AutoOffsetReset.Latest);

    /// <summary>
    ///     Specifies that an error (ERR__AUTO_OFFSET_RESET) must be triggered, when there is no initial offset in the offset store or the
    ///     desired offset is out of range.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder DisableAutoOffsetReset() => WithAutoOffsetReset(AutoOffsetReset.Error);

    /// <summary>
    ///     Sets the static instance id used to enable static group membership. Static group members are able to leave and rejoin a group within
    ///     the configured <see cref="KafkaConsumerConfiguration.SessionTimeoutMs" /> without prompting a group rebalance. This should be used in
    ///     combination with a larger <see cref="KafkaConsumerConfiguration.SessionTimeoutMs" /> to avoid group rebalances caused by transient
    ///     unavailability (e.g. process restarts). Requires broker version &gt;= 2.3.0.
    /// </summary>
    /// <param name="groupInstanceId">
    ///     The static instance id used to enable static group membership.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaConsumerConfigurationBuilder WithGroupInstanceId(string? groupInstanceId);

    /// <summary>
    ///     Sets the partition assignment strategy: <see cref="Confluent.Kafka.PartitionAssignmentStrategy.Range" /> to co-localize the partitions
    ///     of several topics, <see cref="Confluent.Kafka.PartitionAssignmentStrategy.RoundRobin" /> to evenly distribute the partitions among
    ///     the consumer group members, <see cref="Confluent.Kafka.PartitionAssignmentStrategy.CooperativeSticky" /> to evenly distribute the
    ///     partitions and limit minimize the partitions movements. The default is <see cref="Confluent.Kafka.PartitionAssignmentStrategy.Range" />.
    /// </summary>
    /// <param name="partitionAssignmentStrategy">
    ///     The partition assignment strategy.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaConsumerConfigurationBuilder WithPartitionAssignmentStrategy(PartitionAssignmentStrategy? partitionAssignmentStrategy);

    /// <summary>
    ///     Sets the client group session and failure detection timeout (in milliseconds). The consumer sends periodic heartbeats
    ///     <see cref="KafkaConsumerConfiguration.HeartbeatIntervalMs" /> to indicate its liveness to the broker. If no heartbeat is received
    ///     by the broker for a group member within the session timeout, the broker will remove the consumer from the group and trigger a rebalance.
    ///     Also see <see cref="KafkaConsumerConfiguration.MaxPollIntervalMs" />.
    /// </summary>
    /// <param name="sessionTimeoutMs">
    ///     The client group session and failure detection timeout.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaConsumerConfigurationBuilder WithSessionTimeoutMs(int? sessionTimeoutMs);

    /// <summary>
    ///     Sets the interval (in milliseconds) at which the heartbeats have to be sent to the broker.
    /// </summary>
    /// <param name="heartbeatIntervalMs">
    ///     The interval at which the heartbeats have to be sent.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaConsumerConfigurationBuilder WithHeartbeatIntervalMs(int? heartbeatIntervalMs);

    /// <summary>
    ///     Sets the group protocol type.
    /// </summary>
    /// <param name="groupProtocolType">
    ///     The group protocol type.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaConsumerConfigurationBuilder WithGroupProtocolType(string? groupProtocolType);

    /// <summary>
    ///     Sets the interval (in milliseconds) at which the current group coordinator must be queried. If the currently assigned coordinator
    ///     is down the configured query interval will be divided by ten to more quickly recover in case of coordinator reassignment.
    /// </summary>
    /// <param name="coordinatorQueryIntervalMs">
    ///     The interval at which the current group coordinator must be queried.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaConsumerConfigurationBuilder WithCoordinatorQueryIntervalMs(int? coordinatorQueryIntervalMs);

    /// <summary>
    ///     Sets the maximum allowed time (in milliseconds) between calls to consume messages. If this interval is exceeded the consumer is
    ///     considered failed and the group will rebalance in order to reassign the partitions to another consumer group member.<br />
    ///     Warning: Offset commits may be not possible at this point.
    /// </summary>
    /// <param name="maxPollIntervalMs">
    ///     The maximum allowed time between calls to consume messages.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaConsumerConfigurationBuilder WithMaxPollIntervalMs(int? maxPollIntervalMs);

    /// <summary>
    ///     Sets the frequency in milliseconds at which the consumer offsets are committed.
    /// </summary>
    /// <param name="autoCommitIntervalMs">
    ///     The frequency at which the consumer offsets are committed.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaConsumerConfigurationBuilder WithAutoCommitIntervalMs(int? autoCommitIntervalMs);

    /// <summary>
    ///     Sets the minimum number of messages per topic and partition that the underlying library must try to maintain in the local consumer queue.
    /// </summary>
    /// <param name="queuedMinMessages">
    ///     The minimum number of messages that must be maintained in the local consumer queue.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaConsumerConfigurationBuilder WithQueuedMinMessages(int? queuedMinMessages);

    /// <summary>
    ///     Sets the maximum number of kilobytes of queued pre-fetched messages to store in the local consumer queue. This setting applies to
    ///     the single consumer queue, regardless of the number of partitions. This value may be overshot by
    ///     <see cref="KafkaConsumerConfiguration.FetchMaxBytes" />. This property has higher priority than
    ///     <see cref="KafkaConsumerConfiguration.QueuedMinMessages" />.
    /// </summary>
    /// <param name="queuedMaxMessagesKbytes">
    ///     The maximum number of kilobytes of queued pre-fetched messages to store in the local consumer queue.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaConsumerConfigurationBuilder WithQueuedMaxMessagesKbytes(int? queuedMaxMessagesKbytes);

    /// <summary>
    ///     Sets the maximum time (in milliseconds) that the broker may wait to fill the fetch response with enough messages to match the
    ///     size specified by <see cref="KafkaConsumerConfiguration.FetchMinBytes" />.
    /// </summary>
    /// <param name="fetchWaitMaxMs">
    ///     The maximum time that the broker may wait to fill the fetch response.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaConsumerConfigurationBuilder WithFetchWaitMaxMs(int? fetchWaitMaxMs);

    /// <summary>
    ///     Sets the initial maximum number of bytes per topic and partition to request when fetching messages from the broker. If the client
    ///     encounters a message larger than this value it will gradually try to increase it until the entire message can be fetched.
    /// </summary>
    /// <param name="maxPartitionFetchBytes">
    ///     The initial maximum number of bytes per topic and partition to request when fetching messages.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaConsumerConfigurationBuilder WithMaxPartitionFetchBytes(int? maxPartitionFetchBytes);

    /// <summary>
    ///     Sets the maximum amount of data the broker shall return for a fetch request. The messages are fetched in batches by the consumer
    ///     and if the first message batch in the first non-empty partition of the fetch request is larger than this value, then the message
    ///     batch will still be returned to ensure that the consumer can make progress. The maximum message batch size accepted by the broker
    ///     is defined via <c>message.max.bytes</c> (broker config) or <c>max.message.bytes</c> (broker topic config). This value is automatically
    ///     adjusted upwards to be at least <c>message.max.bytes</c> (consumer config).
    /// </summary>
    /// <param name="fetchMaxBytes">
    ///     The maximum amount of data the broker shall return for a fetch request.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaConsumerConfigurationBuilder WithFetchMaxBytes(int? fetchMaxBytes);

    /// <summary>
    ///     Sets the minimum number of bytes that the broker must respond with. If <see cref="KafkaConsumerConfiguration.FetchWaitMaxMs" />
    ///     expires the accumulated data will be sent to the client regardless of this setting.
    /// </summary>
    /// <param name="fetchMinBytes">
    ///     The minimum number of bytes that the broker must respond with.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaConsumerConfigurationBuilder WithFetchMinBytes(int? fetchMinBytes);

    /// <summary>
    ///     Sets how long to postpone the next fetch request for a topic and partition in case of a fetch error.
    /// </summary>
    /// <param name="fetchErrorBackoffMs">
    ///     How long to postpone the next fetch request in case of a fetch error.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaConsumerConfigurationBuilder WithFetchErrorBackoffMs(int? fetchErrorBackoffMs);

    /// <summary>
    ///     Sets a value indicating how to read messages written inside a transaction: <see cref="Confluent.Kafka.IsolationLevel.ReadCommitted" />
    ///     to only return transactional messages which have been committed, or <see cref="Confluent.Kafka.IsolationLevel.ReadUncommitted" /> to
    ///     return all messages, even transactional messages which have been aborted.
    /// </summary>
    /// <param name="isolationLevel">
    ///     A value indicating how to read messages written inside a transaction.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public partial KafkaConsumerConfigurationBuilder WithIsolationLevel(IsolationLevel? isolationLevel);

    /// <summary>
    ///     Enables the verification of the CRC32 of the consumed messages, ensuring no on-the-wire or on-disk corruption to the messages
    ///     occurred. This check comes at slightly increased CPU usage.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder EnableCheckCrcs()
    {
        WithCheckCrcs(true);
        return this;
    }

    /// <summary>
    ///     Disables the verification of the CRC32 of the consumed messages, ensuring no on-the-wire or on-disk corruption to the messages
    ///     occurred. This check comes at slightly increased CPU usage.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder DisableCheckCrcs()
    {
        WithCheckCrcs(false);
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
            ClientSideOffsetStore = _clientSideOffsetStoreSettings ?? configuration.ClientSideOffsetStore,
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
