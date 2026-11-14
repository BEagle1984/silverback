// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    private static readonly TimeSpan LeaveGroupPollTimeout = TimeSpan.FromMilliseconds(100);

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

    // Bounds the polls running the rebalance callbacks and leaving the group before closing, in case the group is never left
    internal TimeSpan LeaveGroupTimeout { get; set; } = TimeSpan.FromSeconds(10);

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

        // Close also releases the native handle, but neither it nor leaving the group can hold the lock: they run the revocation
        // callbacks, which wait for the processing, which may commit from other threads. Instead, from here on only this thread may use
        // the native consumer: the pending native calls complete first and the calls from the other threads fail without reaching it.
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
        finally
        {
            try
            {
                if (!Configuration.IsStaticAssignment)
                    LeaveGroup(confluentConsumer);

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

    // Close runs the pending rebalance callbacks while the consumer is terminating, when librdkafka can hang on an assignment and
    // Confluent.Kafka rejects a changed one. Leaving the group first runs them while the group is still alive. Once unsubscribed, only
    // the queued callbacks and the revocation of the partitions they assign remain, except during a rebalance: librdkafka then defers
    // the unsubscribe until the rebalance completes, and an assignment arriving between the last poll and Close is still run by Close,
    // unless the LeaveGroup request is awaited.
    // With the classic protocol and the cooperative sticky partition assignment strategy, librdkafka sends the LeaveGroup request once
    // the revocation completes, including the commit of the revoked offsets (e.g. by the auto-commit), but skips it if Close comes first,
    // leaving the partitions unassigned until the session times out. It clears the member id when it leaves, so that's awaited as well,
    // which also makes a disconnect during a rebalance wait for the rebalance to complete. With the range and round robin strategies the
    // group is left on Close anyway, a static member (group.instance.id) is meant to keep its membership, and with the consumer protocol
    // (KIP-848) the group is left anyway, while the member id is generated by the client and kept for the lifetime of the consumer.
    // If the group isn't left within the timeout (e.g. a cooperative rebalance waiting for another member), Close revokes the
    // assignment as it did before leaving the group was introduced, while discarding the assignments arriving afterwards.
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Logged, the consumer is closed anyway")]
    private void LeaveGroup(IConsumer<byte[]?, byte[]?> confluentConsumer)
    {
        try
        {
            // The pause discards the prefetched records, which would otherwise be polled one by one
            confluentConsumer.Pause(confluentConsumer.Assignment);
        }
        catch (Exception ex)
        {
            _logger.LogConsumerLeaveGroupError(Consumer, ex);
        }

        try
        {
            confluentConsumer.Unsubscribe();
        }
        catch (Exception ex)
        {
            _logger.LogConsumerLeaveGroupError(Consumer, ex);
            return;
        }

        bool awaitLeaveGroupRequest = Configuration.GroupProtocol != GroupProtocol.Consumer &&
                                      Configuration.PartitionAssignmentStrategy == PartitionAssignmentStrategy.CooperativeSticky &&
                                      string.IsNullOrEmpty(Configuration.GroupInstanceId);
        Stopwatch stopwatch = Stopwatch.StartNew();
        TimeSpan pollTimeout = TimeSpan.Zero;

        while (stopwatch.Elapsed < LeaveGroupTimeout)
        {
            try
            {
                // A poll returns early only with a record, which is discarded without being stored and will be consumed again. An
                // empty poll ran every callback queued until its timeout.
                if (confluentConsumer.Consume(pollTimeout) == null &&
                    confluentConsumer.Assignment.Count == 0 &&
                    (!awaitLeaveGroupRequest || string.IsNullOrEmpty(confluentConsumer.MemberId)))
                {
                    return;
                }
            }
            catch (KafkaException ex) when (ex.Error.IsFatal)
            {
                _logger.LogConsumerLeaveGroupError(Consumer, ex);
                return;
            }
            catch (Exception ex)
            {
                // A failed callback or record doesn't prevent the next ones from running
                _logger.LogConsumerLeaveGroupError(Consumer, ex);
            }

            // A poll waits for its whole timeout after running the callbacks
            pollTimeout = Configuration.PollingTimeout < LeaveGroupPollTimeout ? Configuration.PollingTimeout : LeaveGroupPollTimeout;
        }

        _logger.LogConsumerLeaveGroupTimeout(LeaveGroupTimeout, Consumer);
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
