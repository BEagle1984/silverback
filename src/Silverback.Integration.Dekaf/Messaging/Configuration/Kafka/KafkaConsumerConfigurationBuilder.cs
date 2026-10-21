// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
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
public class KafkaConsumerConfigurationBuilder
    : KafkaClientConfigurationBuilder<KafkaConsumerConfiguration, KafkaConsumerConfigurationBuilder>
{
    private readonly IServiceProvider _serviceProvider;

    private readonly Dictionary<string, KafkaConsumerEndpointConfiguration> _endpoints = [];

    private string? _groupId;

    private string? _groupInstanceId;

    private AutoOffsetReset? _autoOffsetReset;

    private int? _autoCommitIntervalMs;

    private bool? _enablePartitionEof;

    private bool? _commitOffsets;

    private int? _commitOffsetEach;

    private bool? _enableAutoCommit;

    private KafkaOffsetStoreSettings? _clientSideOffsetStoreSettings;

    private bool? _sendOffsetsToTransaction;

    private bool? _enableAutoRecovery;

    private bool? _processPartitionsIndependently;

    private int? _maxDegreeOfParallelism;

    private int? _backpressureLimit;

    private TimeSpan? _pollingTimeout;

    private TimeSpan? _stallDetectionThreshold;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaConsumerConfigurationBuilder" /> class.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> instance.
    /// </param>
    public KafkaConsumerConfigurationBuilder(IServiceProvider serviceProvider)
    {
        _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
    }

    /// <inheritdoc cref="KafkaClientConfigurationBuilder{TConfig,TBuilder}.This" />
    protected override KafkaConsumerConfigurationBuilder This => this;

    /// <summary>
    ///     Sets the client group id string. All clients sharing the same group id belong to the same group.
    /// </summary>
    /// <param name="groupId">
    ///     The client group id string.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder WithGroupId(string? groupId)
    {
        _groupId = groupId;
        return this;
    }

    /// <summary>
    ///     Sets the static group instance id, used to enable static group membership.
    /// </summary>
    /// <param name="groupInstanceId">
    ///     The static group instance id.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder WithGroupInstanceId(string? groupInstanceId)
    {
        _groupInstanceId = groupInstanceId;
        return this;
    }

    /// <summary>
    ///     Sets the action to take when there is no initial offset in the offset store or the desired offset is out of
    ///     range.
    /// </summary>
    /// <param name="autoOffsetReset">
    ///     The <see cref="AutoOffsetReset" />.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder WithAutoOffsetReset(AutoOffsetReset? autoOffsetReset)
    {
        _autoOffsetReset = autoOffsetReset;
        return this;
    }

    /// <summary>
    ///     Sets the frequency in milliseconds at which the stored offsets are committed when auto-commit is enabled.
    /// </summary>
    /// <param name="autoCommitIntervalMs">
    ///     The auto-commit interval in milliseconds.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder WithAutoCommitIntervalMs(int? autoCommitIntervalMs)
    {
        _autoCommitIntervalMs = autoCommitIntervalMs;
        return this;
    }

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
    ///     By default, the name will be generated concatenating the topic name(s).
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
    ///     By default, the name will be generated concatenating the topic name(s).
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

        KafkaConsumerEndpointConfigurationBuilder<TMessage> builder = new(_serviceProvider, name);
        configurationBuilderAction.Invoke(builder);
        KafkaConsumerEndpointConfiguration endpointConfiguration = builder.Build();

        _endpoints.TryAdd(name ?? endpointConfiguration.RawName, endpointConfiguration);

        return this;
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
        _enableAutoCommit = false;
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
        _enableAutoCommit = true;
        return this;
    }

    /// <summary>
    ///     Disable automatic offsets commit. Note: setting this does not prevent the consumer from fetching previously committed start
    ///     offsets.
    /// </summary>
    /// <remarks>
    ///     See also <see cref="CommitOffsetEach" />.
    /// </remarks>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder DisableAutoCommit()
    {
        _enableAutoCommit = false;
        return this;
    }

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
        _enableAutoCommit = false;
        return this;
    }

    /// <summary>
    ///     Specifies that the offsets have to stored in the specified client store, additionally to or instead of being committed to the
    ///     message broker.
    /// </summary>
    /// <remarks>
    ///     The offset stores of the Silverback.Storage packages depend on Silverback.Integration.Kafka and can't be used
    ///     with the Dekaf integration yet: a custom <see cref="IKafkaOffsetStore" /> is required.
    /// </remarks>
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
    /// <remarks>
    ///     The offset stores of the Silverback.Storage packages depend on Silverback.Integration.Kafka and can't be used
    ///     with the Dekaf integration yet: a custom <see cref="IKafkaOffsetStore" /> is required.
    /// </remarks>
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
    ///     Specifies that the consumer should commit the consumed offsets in the same transaction of the produced messages.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder SendOffsetsToTransaction()
    {
        _sendOffsetsToTransaction = true;
        DisableOffsetsCommit();

        return This;
    }

    /// <summary>
    ///     Specifies that the consumer should not commit the consumed offsets in the same transaction of the produced messages.
    ///     This is the default.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder DisableSendOffsetsToTransaction()
    {
        _sendOffsetsToTransaction = false;
        return This;
    }

    /// <summary>
    ///     Specifies that the consumer has to be automatically recycled when a <c>Dekaf.Errors.KafkaException</c>
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
    ///     Specifies that the consumer doesn't have to be automatically recycled when a <c>Dekaf.Errors.KafkaException</c>
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
    ///     The default limit is 50.
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
    ///     Sets the timeout to wait for the consumer to poll for new messages before initiating a new poll. The default is 100 milliseconds.
    /// </summary>
    /// <param name="pollingTimeout">
    ///     The timeout to wait for the consumer to poll for new messages.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder WithPollingTimeout(TimeSpan pollingTimeout)
    {
        _pollingTimeout = Check.GreaterThan(pollingTimeout, nameof(pollingTimeout), TimeSpan.Zero);
        return this;
    }

    /// <summary>
    ///     Sets the maximum time to wait for a message to be consumed before the consumer is considered stale. A stale consumer is
    ///     automatically restarted.
    ///     The default is <c>null</c>, which means that the consumer is never considered stale and is never restarted.
    /// </summary>
    /// <param name="stallDetectionThreshold">
    ///     The maximum time to wait for a message to be consumed before the consumer is considered stale.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder WithStallDetectionThreshold(TimeSpan? stallDetectionThreshold)
    {
        if (stallDetectionThreshold.HasValue)
            Check.GreaterThan(stallDetectionThreshold.Value, nameof(stallDetectionThreshold), TimeSpan.Zero);

        _stallDetectionThreshold = stallDetectionThreshold;
        return this;
    }

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
    ///     Specifies that an error must be triggered, when there is no initial offset in the offset store or the
    ///     desired offset is out of range.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public KafkaConsumerConfigurationBuilder DisableAutoOffsetReset() => WithAutoOffsetReset(AutoOffsetReset.Error);

    /// <summary>
    ///     Builds the <see cref="KafkaConsumerConfiguration" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfiguration" />.
    /// </returns>
    public KafkaConsumerConfiguration Build()
    {
        KafkaConsumerConfiguration configuration = BuildCore();

        configuration = configuration with
        {
            GroupId = _groupId ?? configuration.GroupId,
            GroupInstanceId = _groupInstanceId ?? configuration.GroupInstanceId,
            AutoOffsetReset = _autoOffsetReset ?? configuration.AutoOffsetReset,
            AutoCommitIntervalMs = _autoCommitIntervalMs ?? configuration.AutoCommitIntervalMs,
            EnablePartitionEof = _enablePartitionEof ?? configuration.EnablePartitionEof,
            EnableAutoCommit = _enableAutoCommit ?? configuration.EnableAutoCommit,
            CommitOffsets = _commitOffsets ?? configuration.CommitOffsets,
            CommitOffsetEach = _commitOffsetEach ?? configuration.CommitOffsetEach,
            ClientSideOffsetStore = _clientSideOffsetStoreSettings ?? configuration.ClientSideOffsetStore,
            SendOffsetsToTransaction = _sendOffsetsToTransaction ?? configuration.SendOffsetsToTransaction,
            EnableAutoRecovery = _enableAutoRecovery ?? configuration.EnableAutoRecovery,
            BackpressureLimit = _backpressureLimit ?? configuration.BackpressureLimit,
            PollingTimeout = _pollingTimeout ?? configuration.PollingTimeout,
            StallDetectionThreshold = _stallDetectionThreshold ?? configuration.StallDetectionThreshold,
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

    /// <summary>
    ///     Sets a value indicating whether the partition EOF event must be emitted whenever the consumer reaches the
    ///     end of a partition.
    /// </summary>
    /// <param name="enablePartitionEof">
    ///     A value indicating whether the partition EOF event must be emitted.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaConsumerConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    private KafkaConsumerConfigurationBuilder WithEnablePartitionEof(bool? enablePartitionEof)
    {
        _enablePartitionEof = enablePartitionEof;
        return this;
    }
}
