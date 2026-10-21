// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dekaf.Consumer;
using Dekaf.Errors;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Messages;
using Silverback.Util;
using DekafTopicPartition = Dekaf.TopicPartition;
using DekafTopicPartitionOffset = Dekaf.TopicPartitionOffset;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     Wraps the Dekaf consumer and translates between the Dekaf and the Silverback types.
/// </summary>
/// <remarks>
///     The partitions assignment is driven by the rebalance listener. While a rebalance callback runs, the consumer
///     operations (commit, store, seek, pause, resume) are routed through the <see cref="IRebalanceConsumer" />
///     handed to the callback, since Dekaf's consumer API must not be re-entered from inside its own callbacks.
/// </remarks>
internal sealed class DekafConsumerWrapper : BrokerClient, IDekafConsumerWrapper, IConsumerAwareRebalanceListener
{
    private readonly IDekafClientsFactory _clientsFactory;

    private readonly IBrokerClientCallbacksInvoker _callbacksInvoker;

    private readonly IKafkaOffsetStoreFactory _offsetStoreFactory;

    private readonly IServiceProvider _serviceProvider;

    private readonly ISilverbackLogger _logger;

    private readonly System.Threading.Lock _assignmentLock = new();

    private readonly HashSet<DekafTopicPartition> _assignment = [];

    // The stored offsets not yet committed, needed to commit the partitions being revoked (see CommitInRebalanceScope)
    private readonly ConcurrentDictionary<DekafTopicPartition, long> _pendingOffsets = new();

    // Set only for the duration of a rebalance callback and bound to its thread: an AsyncLocal would flow into the
    // channel reader tasks started by the callback, which would keep using the view after it has expired
    private RebalanceScope? _rebalanceScope;

    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Disposed in DisconnectCoreAsync")]
    private IKafkaConsumer<byte[], byte[]>? _dekafConsumer;

    public DekafConsumerWrapper(
        string name,
        IDekafClientsFactory clientsFactory,
        KafkaConsumerConfiguration configuration,
        IBrokerClientCallbacksInvoker callbacksInvoker,
        IKafkaOffsetStoreFactory offsetStoreFactory,
        IServiceProvider serviceProvider,
        ISilverbackLogger logger)
        : base(name, logger)
    {
        _clientsFactory = Check.NotNull(clientsFactory, nameof(clientsFactory));
        Configuration = Check.NotNull(configuration, nameof(configuration));
        _callbacksInvoker = Check.NotNull(callbacksInvoker, nameof(callbacksInvoker));
        _offsetStoreFactory = Check.NotNull(offsetStoreFactory, nameof(offsetStoreFactory));
        _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
        _logger = Check.NotNull(logger, nameof(logger));
    }

    public KafkaConsumerConfiguration Configuration { get; }

    public IReadOnlyList<TopicPartition> Assignment
    {
        get
        {
            lock (_assignmentLock)
            {
                return [.. _assignment.Select(partition => partition.ToSilverback())];
            }
        }
    }

    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Life cycle externally handled")]
    public KafkaConsumer Consumer
    {
        get => field ?? throw new InvalidOperationException("The consumer is not initialized yet.");
        set => field = Check.NotNull(value, nameof(value));
    }

    private RebalanceScope? CurrentRebalanceScope =>
        Volatile.Read(ref _rebalanceScope) is { } scope && scope.ThreadId == Environment.CurrentManagedThreadId ? scope : null;

    public ConsumerGroupMetadata GetConsumerGroupMetadata() =>
        EnsureConnected(ClientStatus.Initialized).ConsumerGroupMetadata ??
        throw new InvalidOperationException("The consumer group metadata is not available (not part of a consumer group).");

    public KafkaConsumedRecord? Consume(TimeSpan timeout)
    {
        IKafkaConsumer<byte[], byte[]> dekafConsumer = EnsureConnected(ClientStatus.Initialized, ClientStatus.Initializing);

        // The consume loop drives the consumer synchronously from its own long-running thread
        ConsumeResult<byte[], byte[]>? result = dekafConsumer.ConsumeOneAsync(timeout).SafeWait();

        // Copy right away: the result borrows the fetch buffers
        return result.HasValue ? ToConsumedRecord(result.Value) : null;
    }

    public void StoreOffset(TopicPartitionOffset topicPartitionOffset)
    {
        Check.NotNull(topicPartitionOffset, nameof(topicPartitionOffset));

        ConsumerOperations operations = GetOperations(ClientStatus.Initialized, ClientStatus.Disconnecting);
        DekafTopicPartitionOffset offset = topicPartitionOffset.ToDekaf();

        operations.StoreOffset(offset);
        _pendingOffsets[topicPartitionOffset.TopicPartition.ToDekaf()] = offset.Offset;
    }

    // The offsets are always committed explicitly: the parameterless Dekaf commit isn't safe to call concurrently with
    // the poll and, inside the revoke callback, Dekaf skips the stored offsets of the partitions being revoked. The
    // offsets of the partitions no longer owned are dropped, since they can't be committed anymore.
    public void Commit()
    {
        IKafkaConsumer<byte[], byte[]> dekafConsumer = EnsureConnected(ClientStatus.Initialized, ClientStatus.Disconnecting);

        if (!Configuration.CommitOffsets)
            return;

        RebalanceScope? scope = CurrentRebalanceScope;

        // An offset stored while committing stays pending
        KeyValuePair<DekafTopicPartition, long>[] pendingOffsets = [.. _pendingOffsets];
        KeyValuePair<DekafTopicPartition, long>[] committableOffsets;

        lock (_assignmentLock)
        {
            committableOffsets =
            [
                .. pendingOffsets.Where(pair => _assignment.Contains(pair.Key) || scope?.RevokingPartitions.Contains(pair.Key) == true)
            ];
        }

        try
        {
            if (committableOffsets.Length > 0)
            {
                IEnumerable<DekafTopicPartitionOffset> offsets =
                    committableOffsets.Select(pair => new DekafTopicPartitionOffset(pair.Key.Topic, pair.Key.Partition, pair.Value));

                if (scope != null)
                    scope.Consumer.CommitAsync(offsets).SafeWait();
                else
                    dekafConsumer.CommitAsync(offsets).SafeWait();
            }

            foreach (KeyValuePair<DekafTopicPartition, long> pendingOffset in pendingOffsets)
            {
                _pendingOffsets.TryRemove(pendingOffset);
            }

            foreach (KeyValuePair<DekafTopicPartition, long> committedOffset in committableOffsets)
            {
                _logger.LogOffsetCommitted(committedOffset.Key.ToSilverback(committedOffset.Value), Consumer);
            }
        }
        catch (KafkaException ex)
        {
            _logger.LogConsumerCommitError(Consumer, [], ex);
        }
    }

    public void Seek(TopicPartitionOffset topicPartitionOffset)
    {
        Check.NotNull(topicPartitionOffset, nameof(topicPartitionOffset));

        TrySeek(GetOperations(ClientStatus.Initialized), topicPartitionOffset);
    }

    public void Pause(IEnumerable<TopicPartition> partitions)
    {
        Check.NotNull(partitions, nameof(partitions));

        GetOperations(ClientStatus.Initialized).Pause([.. partitions.Select(partition => partition.ToDekaf())]);
    }

    public void Resume(IEnumerable<TopicPartition> partitions)
    {
        Check.NotNull(partitions, nameof(partitions));

        GetOperations(ClientStatus.Initialized).Resume([.. partitions.Select(partition => partition.ToDekaf())]);
    }

    public IReadOnlyList<TopicPartitionOffset> OffsetsForTimes(IEnumerable<TopicPartitionTimestamp> timestampsToSearch, TimeSpan timeout)
    {
        Check.NotNull(timestampsToSearch, nameof(timestampsToSearch));

        IKafkaConsumer<byte[], byte[]> dekafConsumer = EnsureConnected(ClientStatus.Initialized);

        using CancellationTokenSource cancellationTokenSource = new(timeout);

        IReadOnlyDictionary<DekafTopicPartition, long> offsets = dekafConsumer.Offsets
            .GetOffsetsForTimesAsync(timestampsToSearch.Select(timestamp => timestamp.ToDekaf()), cancellationTokenSource.Token)
            .SafeWait();

        return [.. offsets.Select(pair => pair.Key.ToSilverback(pair.Value))];
    }

    ValueTask IConsumerAwareRebalanceListener.OnPartitionsAssignedAsync(
        IRebalanceConsumer consumer,
        IEnumerable<DekafTopicPartition> partitions,
        CancellationToken cancellationToken)
    {
        RunInRebalanceScope(consumer, [], () => OnPartitionsAssigned(partitions));
        return default;
    }

    ValueTask IConsumerAwareRebalanceListener.OnPartitionsRevokedAsync(
        IRebalanceConsumer consumer,
        IEnumerable<DekafTopicPartition> partitions,
        CancellationToken cancellationToken)
    {
        DekafTopicPartition[] revokedPartitions = [.. partitions];
        RunInRebalanceScope(consumer, revokedPartitions, () => OnPartitionsRevoked(revokedPartitions));
        return default;
    }

    ValueTask IConsumerAwareRebalanceListener.OnPartitionsLostAsync(
        IRebalanceConsumer consumer,
        IEnumerable<DekafTopicPartition> partitions,
        CancellationToken cancellationToken)
    {
        // The lost partitions are owned by another member already: their offsets must not be committed
        RunInRebalanceScope(consumer, [], () => OnPartitionsRevoked(partitions));
        return default;
    }

    protected override async ValueTask ConnectCoreAsync()
    {
        lock (_assignmentLock)
        {
            _assignment.Clear();
        }

        IKafkaConsumer<byte[], byte[]> dekafConsumer = _clientsFactory.CreateConsumer(Configuration, this);
        _dekafConsumer = dekafConsumer;

        try
        {
            await dekafConsumer.InitializeAsync().ConfigureAwait(false);

            if (Configuration.IsStaticAssignment)
                PerformStaticAssignment(dekafConsumer);
            else
                Subscribe(dekafConsumer);
        }
        catch
        {
            // A retried connection creates a new client, this one would otherwise keep its background tasks running
            _dekafConsumer = null;
            await dekafConsumer.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    protected override async ValueTask DisconnectCoreAsync()
    {
        IKafkaConsumer<byte[], byte[]>? dekafConsumer = _dekafConsumer;

        if (dekafConsumer == null)
            return;

        try
        {
            if (!Configuration.EnableAutoCommit)
                Commit();
        }
        finally
        {
            try
            {
                await dekafConsumer.CloseAsync().ConfigureAwait(false);
            }
            finally
            {
                await dekafConsumer.DisposeAsync().ConfigureAwait(false);
                _dekafConsumer = null;

                lock (_assignmentLock)
                {
                    _assignment.Clear();
                }
            }
        }
    }

    private static KafkaConsumedRecord ToConsumedRecord(ConsumeResult<byte[], byte[]> result)
    {
        TopicPartitionOffset topicPartitionOffset = new(result.Topic, result.Partition, result.Offset);

        if (result.IsPartitionEof)
            return KafkaConsumedRecord.PartitionEof(topicPartitionOffset);

        return new KafkaConsumedRecord(
            topicPartitionOffset,
            result.IsKeyNull ? null : result.Key,
            result.Value,
            result.Headers.ToSilverbackHeaders(),
            result.Timestamp);
    }

    // Silverback's Offset encodes the logical offsets as negative values, while Dekaf exposes dedicated seek methods
    private static bool TrySeek(ConsumerOperations operations, TopicPartitionOffset topicPartitionOffset)
    {
        Offset offset = topicPartitionOffset.Offset;

        if (offset == Offset.Unset || offset == Offset.Stored)
            return false;

        if (offset == Offset.Beginning)
            operations.SeekToBeginning(topicPartitionOffset.TopicPartition.ToDekaf());
        else if (offset == Offset.End)
            operations.SeekToEnd(topicPartitionOffset.TopicPartition.ToDekaf());
        else
            operations.Seek(topicPartitionOffset.ToDekaf());

        return true;
    }

    private IKafkaConsumer<byte[], byte[]> EnsureConnected(params ClientStatus[] allowedStatuses)
    {
        if (!allowedStatuses.Contains(Status))
            throw new InvalidOperationException("The consumer is not connected.");

        return _dekafConsumer ?? throw new InvalidOperationException("The underlying consumer is not initialized.");
    }

    private ConsumerOperations GetOperations(params ClientStatus[] allowedStatuses)
    {
        IKafkaConsumer<byte[], byte[]> dekafConsumer = EnsureConnected(allowedStatuses);

        return CurrentRebalanceScope is { } scope
            ? new RebalanceConsumerOperations(scope.Consumer, dekafConsumer)
            : new DirectConsumerOperations(dekafConsumer);
    }

    // The rebalance callbacks are invoked in any status but Disconnected, e.g. while connecting or closing
    private ConsumerOperations GetOperationsInRebalanceCallback() =>
        GetOperations(ClientStatus.Initializing, ClientStatus.Initialized, ClientStatus.Disconnecting);

    // Dekaf only logs the exceptions thrown by the rebalance listener: they are handled like the consume errors instead,
    // which the Confluent.Kafka consumer rethrows from Consume, so that the consumer doesn't keep running with a
    // partially applied assignment
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Handled via auto recovery")]
    private void RunInRebalanceScope(IRebalanceConsumer consumer, IReadOnlyCollection<DekafTopicPartition> revokingPartitions, Action action)
    {
        Volatile.Write(ref _rebalanceScope, new RebalanceScope(consumer, Environment.CurrentManagedThreadId, revokingPartitions));

        try
        {
            action.Invoke();
        }
        catch (Exception ex)
        {
            if (!Configuration.EnableAutoRecovery)
            {
                _logger.LogKafkaExceptionNoAutoRecovery(Consumer, ex);
                return;
            }

            _logger.LogKafkaExceptionAutoRecovery(Consumer, ex);
            Consumer.TriggerReconnectAsync().FireAndForget();
        }
        finally
        {
            Volatile.Write(ref _rebalanceScope, null);
        }
    }

    private void OnPartitionsAssigned(IEnumerable<DekafTopicPartition> partitions)
    {
        List<TopicPartition> topicPartitions;

        lock (_assignmentLock)
        {
            topicPartitions = [.. partitions.Where(_assignment.Add).Select(partition => partition.ToSilverback())];
        }

        if (topicPartitions.Count == 0)
            return;

        topicPartitions.ForEach(topicPartition => _logger.LogPartitionAssigned(topicPartition, Consumer));

        List<TopicPartitionOffset>? topicPartitionOffsets = null;

        _callbacksInvoker.Invoke<IKafkaPartitionsAssignedCallback>(callback =>
        {
            IEnumerable<TopicPartitionOffset>? result = callback.OnPartitionsAssigned(topicPartitions, Consumer);

            if (result != null)
                topicPartitionOffsets = [.. result];
        });

        topicPartitionOffsets ??= [.. topicPartitions.Select(partition => new TopicPartitionOffset(partition, Offset.Unset))];

        ConsumerOperations operations = GetOperationsInRebalanceCallback();

        foreach (TopicPartitionOffset topicPartitionOffset in Consumer.OnPartitionsAssigned(topicPartitionOffsets))
        {
            if (TrySeek(operations, topicPartitionOffset))
                _logger.LogPartitionOffsetReset(topicPartitionOffset, Consumer);
        }
    }

    private void OnPartitionsRevoked(IEnumerable<DekafTopicPartition> partitions)
    {
        List<TopicPartitionOffset> topicPartitionOffsets;

        lock (_assignmentLock)
        {
            topicPartitionOffsets = [.. partitions.Where(_assignment.Remove).Select(partition => partition.ToSilverback(Offset.Unset))];
        }

        if (topicPartitionOffsets.Count == 0)
            return;

        Consumer.OnPartitionsRevoked(topicPartitionOffsets);

        topicPartitionOffsets.ForEach(topicPartitionOffset => _logger.LogPartitionRevoked(topicPartitionOffset, Consumer));

        _callbacksInvoker.Invoke<IKafkaPartitionsRevokedCallback>(callback => callback.OnPartitionsRevoked(topicPartitionOffsets, Consumer));
    }

    private void PerformStaticAssignment(IKafkaConsumer<byte[], byte[]> dekafConsumer)
    {
        StoredOffsetsLoader storedOffsetsLoader = new(_offsetStoreFactory, Configuration, _serviceProvider);
        TopicPartitionOffset[] assignment =
        [
            .. Configuration.Endpoints
                .SelectMany(endpoint => endpoint.TopicPartitions)
                .Select(storedOffsetsLoader.ApplyStoredOffset)
        ];
        DekafTopicPartition[] partitions = [.. assignment.Select(topicPartitionOffset => topicPartitionOffset.TopicPartition.ToDekaf())];

        // A manual assignment doesn't trigger the rebalance listener: register it upfront, so that StartCoreAsync
        // starts reading these partitions
        lock (_assignmentLock)
        {
            _assignment.UnionWith(partitions);
        }

        dekafConsumer.Partitions.Assign(partitions);

        DirectConsumerOperations operations = new(dekafConsumer);

        foreach (TopicPartitionOffset topicPartitionOffset in assignment)
        {
            TrySeek(operations, topicPartitionOffset);
            _logger.LogPartitionStaticallyAssigned(topicPartitionOffset, Consumer);
        }
    }

    private void Subscribe(IKafkaConsumer<byte[], byte[]> dekafConsumer) =>
        dekafConsumer.Subscribe(
        [
            .. Configuration.Endpoints
                .SelectMany(endpoint => endpoint.TopicPartitions)
                .Select(topicPartitionOffset => topicPartitionOffset.Topic)
                .Distinct()
        ]);

    private sealed record RebalanceScope(IRebalanceConsumer Consumer, int ThreadId, IReadOnlyCollection<DekafTopicPartition> RevokingPartitions);

    /// <summary>
    ///     The consumer operations that must be routed through the <see cref="IRebalanceConsumer" /> while a
    ///     rebalance callback runs.
    /// </summary>
    private abstract class ConsumerOperations
    {
        private readonly IKafkaConsumer<byte[], byte[]> _dekafConsumer;

        protected ConsumerOperations(IKafkaConsumer<byte[], byte[]> dekafConsumer)
        {
            _dekafConsumer = dekafConsumer;
        }

        public abstract void StoreOffset(DekafTopicPartitionOffset offset);

        public abstract void Seek(DekafTopicPartitionOffset offset);

        public abstract void SeekToEnd(DekafTopicPartition partition);

        public abstract void Pause(DekafTopicPartition[] partitions);

        public abstract void Resume(DekafTopicPartition[] partitions);

        // Dekaf seeks to offset 0, which is out of range (and then reset according to the auto offset reset policy) as
        // soon as the retention deleted the first segment: the log start offset is resolved instead
        public void SeekToBeginning(DekafTopicPartition partition)
        {
            long logStartOffset = _dekafConsumer.Offsets.QueryWatermarkOffsetsAsync(partition).SafeWait().Low;
            Seek(new DekafTopicPartitionOffset(partition.Topic, partition.Partition, logStartOffset));
        }
    }

    private sealed class DirectConsumerOperations : ConsumerOperations
    {
        private readonly IKafkaConsumer<byte[], byte[]> _consumer;

        public DirectConsumerOperations(IKafkaConsumer<byte[], byte[]> consumer)
            : base(consumer)
        {
            _consumer = consumer;
        }

        public override void StoreOffset(DekafTopicPartitionOffset offset) => _consumer.StoreOffset(offset);

        public override void Seek(DekafTopicPartitionOffset offset) => _consumer.Positions.Seek(offset);

        public override void SeekToEnd(DekafTopicPartition partition) => _consumer.Positions.SeekToEnd(partition);

        public override void Pause(DekafTopicPartition[] partitions) => _consumer.Partitions.Pause(partitions);

        public override void Resume(DekafTopicPartition[] partitions) => _consumer.Partitions.Resume(partitions);
    }

    private sealed class RebalanceConsumerOperations : ConsumerOperations
    {
        private readonly IRebalanceConsumer _consumer;

        public RebalanceConsumerOperations(IRebalanceConsumer consumer, IKafkaConsumer<byte[], byte[]> dekafConsumer)
            : base(dekafConsumer)
        {
            _consumer = consumer;
        }

        public override void StoreOffset(DekafTopicPartitionOffset offset) => _consumer.StoreOffset(offset);

        public override void Seek(DekafTopicPartitionOffset offset) => _consumer.Seek(offset);

        public override void SeekToEnd(DekafTopicPartition partition) => _consumer.SeekToEnd(partition);

        public override void Pause(DekafTopicPartition[] partitions) => _consumer.Pause(partitions);

        public override void Resume(DekafTopicPartition[] partitions) => _consumer.Resume(partitions);
    }
}
