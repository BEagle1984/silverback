// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka;

internal class ConfluentConsumerWrapper : BrokerClient, IConfluentConsumerWrapper
{
    private readonly IBrokerClientCallbacksInvoker _brokerClientCallbacksInvoker;

    private readonly IKafkaOffsetStoreFactory _offsetStoreFactory;

    private readonly IServiceProvider _serviceProvider;

    private readonly ISilverbackLogger _logger;

    private readonly IConfluentConsumerBuilder _consumerBuilder;

    private readonly IConfluentAdminClientFactory _adminClientFactory;

    // Guards the native calls that can run concurrently with the disconnect, which releases the native consumer
    private readonly System.Threading.Lock _confluentConsumerLock = new();

    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Life cycle externally handled")]
    private IConsumer<byte[]?, byte[]?>? _confluentConsumer;

    private int _disconnectingThreadId;

    private IAdminClient? _adminClient;

    public ConfluentConsumerWrapper(
        string name,
        IConfluentConsumerBuilder consumerBuilder,
        KafkaConsumerConfiguration configuration,
        IConfluentAdminClientFactory adminClientFactory,
        IBrokerClientCallbacksInvoker brokerClientCallbacksInvoker,
        IKafkaOffsetStoreFactory offsetStoreFactory,
        IServiceProvider serviceProvider,
        ISilverbackLogger logger)
        : base(name, logger)
    {
        Configuration = Check.NotNull(configuration, nameof(configuration));
        _brokerClientCallbacksInvoker = Check.NotNull(brokerClientCallbacksInvoker, nameof(brokerClientCallbacksInvoker));
        _offsetStoreFactory = Check.NotNull(offsetStoreFactory, nameof(offsetStoreFactory));
        _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
        _logger = Check.NotNull(logger, nameof(logger));

        _consumerBuilder = Check.NotNull(consumerBuilder, nameof(consumerBuilder))
            .SetConfig(configuration.ToConfluentConfig())
            .SetEventsHandlers(this, Configuration, _brokerClientCallbacksInvoker, _logger);
        _adminClientFactory = Check.NotNull(adminClientFactory, nameof(adminClientFactory));
    }

    public KafkaConsumerConfiguration Configuration { get; }

    public IReadOnlyList<TopicPartition> Assignment
    {
        get
        {
            lock (_confluentConsumerLock)
            {
                return (IReadOnlyList<TopicPartition>?)GetConfluentConsumerOrDefault()?.Assignment ?? [];
            }
        }
    }

    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Life cycle externally handled")]
    public KafkaConsumer Consumer
    {
        get => field ?? throw new InvalidOperationException("The consumer is not initialized yet.");
        set => field = Check.NotNull(value, nameof(value));
    }

    public IConsumerGroupMetadata GetConsumerGroupMetadata()
    {
        if (Status != ClientStatus.Initialized)
            throw new InvalidOperationException("The consumer is not connected.");

        lock (_confluentConsumerLock)
        {
            return GetConfluentConsumer().ConsumerGroupMetadata;
        }
    }

    public ConsumeResult<byte[]?, byte[]?>? Consume(TimeSpan timeout)
    {
        if (Status is not (ClientStatus.Initialized or ClientStatus.Initializing))
            throw new InvalidOperationException("The consumer is not connected.");

        if (_confluentConsumer == null)
            throw new InvalidOperationException("The underlying consumer is not initialized.");

        // Not synchronized: only the consume loop polls and it's stopped before disconnecting. The poll blocks and runs the
        // rebalance callbacks, which wait for the processing, so it must never hold the lock.
        return _confluentConsumer.Consume(timeout);
    }

    public void StoreOffset(TopicPartitionOffset topicPartitionOffset)
    {
        if (Status is not (ClientStatus.Initialized or ClientStatus.Disconnecting))
            throw new InvalidOperationException("The consumer is not connected.");

        lock (_confluentConsumerLock)
        {
            GetConfluentConsumer().StoreOffset(topicPartitionOffset);
        }
    }

    public void Commit()
    {
        if (Status is not ClientStatus.Initialized and not ClientStatus.Disconnecting)
            throw new InvalidOperationException("The consumer is not connected.");

        try
        {
            List<TopicPartitionOffset> offsets;

            // The commit blocks until the broker responds: holding the lock only delays a concurrent disconnect or native call
            lock (_confluentConsumerLock)
            {
                IConsumer<byte[]?, byte[]?> confluentConsumer = GetConfluentConsumer();

                if (!Configuration.CommitOffsets)
                    return;

                offsets = confluentConsumer.Commit();
            }

            CommittedOffsets committedOffsets = new(
                [.. offsets.Select(offset => new TopicPartitionOffsetError(offset, new Error(ErrorCode.NoError)))],
                new Error(ErrorCode.NoError));

            _brokerClientCallbacksInvoker.Invoke<IKafkaOffsetCommittedCallback>(callback =>
                callback.OnOffsetsCommitted(committedOffsets, Consumer));
        }
        catch (TopicPartitionOffsetException ex)
        {
            foreach (TopicPartitionOffsetError result in ex.Results.Where(result => result.Error.IsError))
            {
                _logger.LogOffsetCommitError(result, Consumer);
            }

            _brokerClientCallbacksInvoker.Invoke<IKafkaOffsetCommittedCallback>(callback =>
                callback.OnOffsetsCommitted(new CommittedOffsets(ex.Results, ex.Error), Consumer));
        }
        catch (KafkaException ex)
        {
            if (ex.Error.Code != ErrorCode.Local_NoOffset)
                _logger.LogConsumerCommitError(Consumer, [], ex);

            _brokerClientCallbacksInvoker.Invoke<IKafkaOffsetCommittedCallback>(callback =>
                callback.OnOffsetsCommitted(new CommittedOffsets(null, ex.Error), Consumer));
        }
    }

    public void Seek(TopicPartitionOffset topicPartitionOffset)
    {
        if (Status != ClientStatus.Initialized)
            throw new InvalidOperationException("The consumer is not connected.");

        lock (_confluentConsumerLock)
        {
            GetConfluentConsumer().Seek(topicPartitionOffset);
        }
    }

    public void Pause(IEnumerable<TopicPartition> partitions)
    {
        if (Status != ClientStatus.Initialized)
            throw new InvalidOperationException("The consumer is not connected.");

        lock (_confluentConsumerLock)
        {
            GetConfluentConsumer().Pause(partitions);
        }
    }

    public void Resume(IEnumerable<TopicPartition> partitions)
    {
        if (Status != ClientStatus.Initialized)
            throw new InvalidOperationException("The consumer is not connected.");

        lock (_confluentConsumerLock)
        {
            GetConfluentConsumer().Resume(partitions);
        }
    }

    public IReadOnlyList<TopicPartitionOffset> OffsetsForTimes(IEnumerable<TopicPartitionTimestamp> timestampsToSearch, TimeSpan timeout)
    {
        if (Status != ClientStatus.Initialized)
            throw new InvalidOperationException("The consumer is not connected.");

        // The lookup blocks until the broker responds or times out: holding the lock only delays a concurrent disconnect or native call
        lock (_confluentConsumerLock)
        {
            return GetConfluentConsumer().OffsetsForTimes(timestampsToSearch, timeout);
        }
    }

    protected override async ValueTask ConnectCoreAsync()
    {
        _confluentConsumer = _consumerBuilder.Build();

        if (Configuration.IsStaticAssignment)
            await PerformStaticAssignmentAsync().ConfigureAwait(false);
        else
            Subscribe();
    }

    protected override ValueTask DisconnectCoreAsync()
    {
        IConsumer<byte[]?, byte[]?> confluentConsumer;

        // Close also releases the native handle, but it can't hold the lock: it runs the revocation callbacks, which wait for the
        // processing, which may commit from other threads. Instead, from here on only this thread may use the native consumer: the
        // pending native calls complete first and the calls from the other threads fail without reaching it.
        lock (_confluentConsumerLock)
        {
            if (_confluentConsumer == null)
                return default;

            confluentConsumer = _confluentConsumer;
            _disconnectingThreadId = Environment.CurrentManagedThreadId;
        }

        try
        {
            if (!Configuration.EnableAutoCommit)
                Commit();
        }
        catch (InvalidOperationException exception) when (exception.Message.Equals("The consumer is not connected.", StringComparison.OrdinalIgnoreCase))
        {
            // The consumer is already disconnected, so we can ignore this exception.
        }
        finally
        {
            try
            {
                confluentConsumer.Close();
            }
            finally
            {
                lock (_confluentConsumerLock)
                {
                    _confluentConsumer = null;
                    _disconnectingThreadId = 0;
                }

                // No other thread can reach the native consumer anymore (it's a no-op unless Close failed)
                confluentConsumer.Dispose();
            }
        }

        return default;
    }

    private async ValueTask PerformStaticAssignmentAsync()
    {
        List<TopicPartitionOffset> assignment = [];

        try
        {
            foreach (KafkaConsumerEndpointConfiguration endpointConfiguration in Configuration.Endpoints)
            {
                await foreach (TopicPartitionOffset topicPartitionOffset in GetAssignmentAsync(endpointConfiguration).ConfigureAwait(false))
                {
                    assignment.Add(topicPartitionOffset);
                }
            }
        }
        finally
        {
            _adminClient?.Dispose();
            _adminClient = null;
        }

        _confluentConsumer!.Assign(assignment);

        assignment.ForEach(topicPartitionOffset => _logger.LogPartitionStaticallyAssigned(topicPartitionOffset, Consumer));
    }

    private async IAsyncEnumerable<TopicPartitionOffset> GetAssignmentAsync(KafkaConsumerEndpointConfiguration endpointConfiguration)
    {
        StoredOffsetsLoader storedOffsetsLoader = new(_offsetStoreFactory, Configuration, _serviceProvider);

        foreach (TopicPartitionOffset topicPartitionOffset in endpointConfiguration.TopicPartitions)
        {
            if (topicPartitionOffset.Partition == Partition.Any && endpointConfiguration.PartitionOffsetsProvider != null)
            {
                IEnumerable<TopicPartitionOffset> providedTopicPartitionOffsets =
                    await GetAssignmentViaProviderAsync(endpointConfiguration.PartitionOffsetsProvider, topicPartitionOffset).ConfigureAwait(false);

                foreach (TopicPartitionOffset providedTopicPartitionOffset in providedTopicPartitionOffsets)
                {
                    yield return storedOffsetsLoader.ApplyStoredOffset(providedTopicPartitionOffset);
                }
            }
            else
            {
                yield return storedOffsetsLoader.ApplyStoredOffset(topicPartitionOffset);
            }
        }
    }

    private async ValueTask<IEnumerable<TopicPartitionOffset>> GetAssignmentViaProviderAsync(
        Func<IReadOnlyCollection<TopicPartition>, ValueTask<IEnumerable<TopicPartitionOffset>>> partitionOffsetsProvider,
        TopicPartitionOffset topicPartitionOffset)
    {
        _adminClient ??= _adminClientFactory.GetClient(Configuration.ToConfluentAdminClientConfig());

        List<TopicPartition> availablePartitions =
        [
            .. _adminClient
                .GetMetadata(topicPartitionOffset.Topic, Configuration.GetMetadataTimeout)
                .Topics[0].Partitions
                .Select(metadata => new TopicPartition(topicPartitionOffset.Topic, metadata.PartitionId))
        ];

        return await partitionOffsetsProvider.Invoke(availablePartitions).ConfigureAwait(false);
    }

    // Must be called while holding the lock. While disconnecting, only the disconnecting thread can use the native consumer.
    private IConsumer<byte[]?, byte[]?>? GetConfluentConsumerOrDefault() =>
        _disconnectingThreadId == 0 || _disconnectingThreadId == Environment.CurrentManagedThreadId ? _confluentConsumer : null;

    private IConsumer<byte[]?, byte[]?> GetConfluentConsumer() =>
        GetConfluentConsumerOrDefault() ?? throw new InvalidOperationException("The underlying consumer is not initialized.");

    private void Subscribe() =>
        _confluentConsumer!.Subscribe(
            Configuration.Endpoints
                .SelectMany(endpoint => endpoint.TopicPartitions)
                .Select(topicPartitionOffset => topicPartitionOffset.Topic)
                .Distinct());
}
