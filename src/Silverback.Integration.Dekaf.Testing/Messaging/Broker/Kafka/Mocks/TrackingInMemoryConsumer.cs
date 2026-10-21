// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dekaf.Consumer;
using Dekaf.Telemetry;
using Dekaf.Testing;
using Silverback.Util;
using DekafTopicPartition = Dekaf.TopicPartition;
using DekafTopicPartitionOffset = Dekaf.TopicPartitionOffset;
using DekafTopicPartitionTimestamp = Dekaf.TopicPartitionTimestamp;
using WatermarkOffsets = Dekaf.WatermarkOffsets;

namespace Silverback.Messaging.Broker.Kafka.Mocks;

/// <summary>
///     Decorates the Dekaf.Testing <see cref="InMemoryConsumer{TKey,TValue}" /> to make it behave like the real
///     client where Silverback relies on it.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <description>
///                 The in-memory consumer doesn't invoke the rebalance listener. The assignment is compared after
///                 each poll and the differences are reported to the listener. Unlike the real client, the revoked
///                 partitions are reported after the fact, when their stored offsets have already been discarded.
///             </description>
///         </item>
///         <item>
///             <description>
///                 The in-memory consumer commits the stored offsets only when it is closed. With auto-commit enabled,
///                 every stored offset is committed right away, as if the auto-commit interval was zero, so that the
///                 tests can observe the committed offsets while the consumer is running.
///             </description>
///         </item>
///     </list>
/// </remarks>
internal sealed class TrackingInMemoryConsumer : IKafkaConsumer<byte[], byte[]>, IRebalanceConsumer, IConsumerPositions
{
    private readonly InMemoryConsumer<byte[], byte[]> _innerConsumer;

    private readonly IConsumerAwareRebalanceListener _rebalanceListener;

    private readonly bool _enableAutoCommit;

    private readonly ConcurrentDictionary<DekafTopicPartition, long> _storedOffsets = new();

    // The positions set by the assignment or a seek, considered as consumed until an offset is stored
    private readonly ConcurrentDictionary<DekafTopicPartition, long> _consumedBaselines = new();

    private readonly HashSet<DekafTopicPartition> _notifiedAssignment = [];

    private readonly System.Threading.Lock _notifiedAssignmentLock = new();

    private bool _isNotifyingAssignmentChange;

    public TrackingInMemoryConsumer(
        InMemoryConsumer<byte[], byte[]> innerConsumer,
        IConsumerAwareRebalanceListener rebalanceListener,
        string? groupId,
        bool enableAutoCommit)
    {
        _innerConsumer = innerConsumer;
        _rebalanceListener = rebalanceListener;
        GroupId = groupId;
        _enableAutoCommit = enableAutoCommit;
    }

    public string? GroupId { get; }

    public bool IsClosed { get; private set; }

    public IReadOnlySet<string> Subscription => _innerConsumer.Subscription;

    public string? SubscriptionPattern => _innerConsumer.SubscriptionPattern;

    public IReadOnlySet<DekafTopicPartition> Assignment => _innerConsumer.Assignment;

    public IReadOnlySet<DekafTopicPartition> Paused => _innerConsumer.Paused;

    public string? MemberId => _innerConsumer.MemberId;

    public ConsumerGroupMetadata? ConsumerGroupMetadata => _innerConsumer.ConsumerGroupMetadata;

    // The seeks are tracked to know which offsets have been consumed
    public IConsumerPositions Positions => this;

    public IConsumerPartitions Partitions => _innerConsumer.Partitions;

    public IConsumerOffsets Offsets => _innerConsumer.Offsets;

    // The cluster reassigns the partitions immediately, the listener is notified at the next poll
    public bool HasPendingAssignmentChange
    {
        get
        {
            if (Volatile.Read(ref _isNotifyingAssignmentChange))
                return true;

            lock (_notifiedAssignmentLock)
            {
                return !_innerConsumer.Assignment.SetEquals(_notifiedAssignment);
            }
        }
    }

    public long? GetStoredOffset(DekafTopicPartition topicPartition)
    {
        if (_storedOffsets.TryGetValue(topicPartition, out long offset))
            return offset;

        return _consumedBaselines.TryGetValue(topicPartition, out long baseline) ? baseline : null;
    }

    public ValueTask InitializeAsync(CancellationToken cancellationToken = default) => _innerConsumer.InitializeAsync(cancellationToken);

    public void Subscribe(params string[] topics) => _innerConsumer.Subscribe(topics);

    public void Subscribe(Func<string, bool> topicFilter) => _innerConsumer.Subscribe(topicFilter);

    public void SubscribePattern(string pattern) => _innerConsumer.SubscribePattern(pattern);

    public void Unsubscribe() => _innerConsumer.Unsubscribe();

    public IAsyncEnumerable<ConsumeResult<byte[], byte[]>> ConsumeAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Only ConsumeOneAsync reports the assignment changes.");

    public async ValueTask<ConsumeResult<byte[], byte[]>?> ConsumeOneAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        ConsumeResult<byte[], byte[]>? result = await _innerConsumer.ConsumeOneAsync(timeout, cancellationToken).ConfigureAwait(false);

        if (!TryBeginAssignmentChange(out IReadOnlySet<DekafTopicPartition> current, out DekafTopicPartition[] revoked, out DekafTopicPartition[] assigned))
            return result;

        try
        {
            if (revoked.Length > 0)
            {
                foreach (DekafTopicPartition partition in revoked)
                {
                    _storedOffsets.TryRemove(partition, out _);
                    _consumedBaselines.TryRemove(partition, out _);
                }

                await _rebalanceListener.OnPartitionsRevokedAsync(this, revoked, cancellationToken).ConfigureAwait(false);
            }

            // The real client reports the assignment before fetching from the new partitions: the record is fetched
            // again from the starting offset. A record of a partition revoked in the meantime would never be returned.
            if (result is { } record)
            {
                DekafTopicPartition recordPartition = new(record.Topic, record.Partition);

                if (assigned.Contains(recordPartition))
                    _innerConsumer.Positions.Seek(new DekafTopicPartitionOffset(record.Topic, record.Partition, record.Offset));

                if (assigned.Contains(recordPartition) || !current.Contains(recordPartition))
                    result = null;
            }

            // The in-memory consumer computes the positions when subscribing only, while the real client starts
            // reading a newly assigned partition from the committed offset
            foreach (DekafTopicPartition partition in assigned)
            {
                await SeekToStartingOffsetAsync(partition, cancellationToken).ConfigureAwait(false);
            }

            if (assigned.Length > 0)
                await _rebalanceListener.OnPartitionsAssignedAsync(this, assigned, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            Volatile.Write(ref _isNotifyingAssignmentChange, false);
        }

        return result;
    }

    public IAsyncEnumerable<ConsumeBatch<byte[], byte[]>> ConsumeBatchAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Only ConsumeOneAsync reports the assignment changes.");

    public IAsyncEnumerable<ConsumeRawBatch> ConsumeRawBatchAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Only ConsumeOneAsync reports the assignment changes.");

    public void RegisterMetricForSubscription(ApplicationTelemetryMetric metric) => _innerConsumer.RegisterMetricForSubscription(metric);

    public void UnregisterMetricFromSubscription(string name) => _innerConsumer.UnregisterMetricFromSubscription(name);

    public ValueTask CommitAsync(CancellationToken cancellationToken = default) => _innerConsumer.CommitAsync(cancellationToken);

    public ValueTask CommitAsync(IEnumerable<DekafTopicPartitionOffset> offsets, CancellationToken cancellationToken = default) =>
        _innerConsumer.CommitAsync(offsets, cancellationToken);

    public void StoreOffset(ConsumeResult<byte[], byte[]> result) =>
        StoreOffset(new DekafTopicPartitionOffset(result.Topic, result.Partition, result.Offset + 1));

    public void StoreOffset(DekafTopicPartitionOffset offset)
    {
        _innerConsumer.StoreOffset(offset);
        _storedOffsets[new DekafTopicPartition(offset.Topic, offset.Partition)] = offset.Offset;

        // The in-memory commit completes synchronously, blocking is therefore safe (and the interface method is sync)
        if (_enableAutoCommit)
            _innerConsumer.CommitAsync([offset]).SafeWait();
    }

    public ValueTask<long?> GetCommittedOffsetAsync(DekafTopicPartition partition, CancellationToken cancellationToken = default) =>
        _innerConsumer.Positions.GetCommittedOffsetAsync(partition, cancellationToken);

    public long? GetPosition(DekafTopicPartition partition) => _innerConsumer.Positions.GetPosition(partition);

    public void Seek(DekafTopicPartitionOffset offset)
    {
        _innerConsumer.Positions.Seek(offset);
        ResetConsumedBaseline(new DekafTopicPartition(offset.Topic, offset.Partition));
    }

    public void SeekToBeginning(params DekafTopicPartition[] partitions)
    {
        _innerConsumer.Positions.SeekToBeginning(partitions);
        Array.ForEach(partitions, ResetConsumedBaseline);
    }

    public void SeekToEnd(params DekafTopicPartition[] partitions)
    {
        _innerConsumer.Positions.SeekToEnd(partitions);
        Array.ForEach(partitions, ResetConsumedBaseline);
    }

    public void Pause(params DekafTopicPartition[] partitions) => _innerConsumer.Partitions.Pause(partitions);

    public void Resume(params DekafTopicPartition[] partitions) => _innerConsumer.Partitions.Resume(partitions);

    public ValueTask<IReadOnlyDictionary<DekafTopicPartition, long>> GetOffsetsForTimesAsync(
        IEnumerable<DekafTopicPartitionTimestamp> timestampsToSearch,
        CancellationToken cancellationToken = default) =>
        _innerConsumer.Offsets.GetOffsetsForTimesAsync(timestampsToSearch, cancellationToken);

    public WatermarkOffsets? GetWatermarkOffsets(DekafTopicPartition topicPartition) => _innerConsumer.Offsets.GetWatermarkOffsets(topicPartition);

    public ValueTask<WatermarkOffsets> QueryWatermarkOffsetsAsync(DekafTopicPartition topicPartition, CancellationToken cancellationToken = default) =>
        _innerConsumer.Offsets.QueryWatermarkOffsetsAsync(topicPartition, cancellationToken);

    public async ValueTask CloseAsync(CancellationToken cancellationToken = default)
    {
        IsClosed = true;
        await _innerConsumer.CloseAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask CloseAsync(ConsumerCloseOptions options, CancellationToken cancellationToken = default)
    {
        IsClosed = true;
        await _innerConsumer.CloseAsync(options, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        IsClosed = true;
        await _innerConsumer.DisposeAsync().ConfigureAwait(false);
    }

    private bool TryBeginAssignmentChange(
        out IReadOnlySet<DekafTopicPartition> current,
        out DekafTopicPartition[] revoked,
        out DekafTopicPartition[] assigned)
    {
        lock (_notifiedAssignmentLock)
        {
            current = _innerConsumer.Assignment;

            if (current.SetEquals(_notifiedAssignment))
            {
                revoked = [];
                assigned = [];
                return false;
            }

            IReadOnlySet<DekafTopicPartition> currentAssignment = current;
            revoked = [.. _notifiedAssignment.Where(partition => !currentAssignment.Contains(partition))];
            assigned = [.. currentAssignment.Where(partition => !_notifiedAssignment.Contains(partition))];
            _notifiedAssignment.Clear();
            _notifiedAssignment.UnionWith(currentAssignment);
            Volatile.Write(ref _isNotifyingAssignmentChange, true);
            return true;
        }
    }

    private async ValueTask SeekToStartingOffsetAsync(DekafTopicPartition partition, CancellationToken cancellationToken)
    {
        long? committedOffset = await _innerConsumer.Positions.GetCommittedOffsetAsync(partition, cancellationToken).ConfigureAwait(false);

        // Without a committed offset the position resolved according to the auto offset reset when subscribing is kept:
        // resolving it again here would skip the messages produced since the consumer connected
        if (committedOffset != null)
            Seek(new DekafTopicPartitionOffset(partition.Topic, partition.Partition, committedOffset.Value));
        else
            ResetConsumedBaseline(partition);
    }

    private void ResetConsumedBaseline(DekafTopicPartition partition)
    {
        _storedOffsets.TryRemove(partition, out _);

        if (_innerConsumer.Positions.GetPosition(partition) is { } position)
            _consumedBaselines[partition] = position;
        else
            _consumedBaselines.TryRemove(partition, out _);
    }
}
