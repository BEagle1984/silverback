// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     Tracks whether the consumer caught up with the partitions assigned to it (see <see cref="CatchUpMode" />).
/// </summary>
/// <remarks>
///     A partition is caught up when its end was polled and every record polled before it is committed (or skipped), which is
///     determined comparing the last record polled before the end with the first uncommitted offset. The end of partition isn't
///     reported again when the records are consumed again (e.g. after a rollback), so it's tracked once per assignment. The consumer is
///     caught up when all the partitions assigned to it until then are, which happens once per connection. The partitions assigned
///     afterward are ignored. With a dynamic assignment, at least one partition must be assigned: a member joining a group may receive
///     an empty assignment first and its partitions in a later rebalance (e.g. with the cooperative sticky assignor).
/// </remarks>
internal sealed class CatchUpTracker
{
    private readonly bool _isStaticAssignment;

    private readonly OffsetsTracker _offsetsTracker;

    private readonly System.Threading.Lock _lock = new();

    private readonly Dictionary<TopicPartition, PartitionState> _partitions = [];

    private TaskCompletionSource _caughtUpTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private bool _isAssigned;

    private bool _isCaughtUp;

    private int _generation;

    public CatchUpTracker(CatchUpMode mode, bool isStaticAssignment, OffsetsTracker offsetsTracker)
    {
        Mode = mode;
        _isStaticAssignment = isStaticAssignment;
        _offsetsTracker = Check.NotNull(offsetsTracker);
    }

    public CatchUpMode Mode { get; }

    public bool IsEnabled => Mode != CatchUpMode.None;

    /// <summary>
    ///     Starts a new catch-up, when a new native consumer is connected.
    /// </summary>
    public void Reset()
    {
        if (!IsEnabled)
            return;

        lock (_lock)
        {
            _partitions.Clear();
            _isAssigned = false;
            _generation++;

            // The pending waiters keep waiting for the new catch-up
            if (_isCaughtUp)
            {
                _isCaughtUp = false;
                _caughtUpTaskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }
    }

    public bool IsCurrent(int generation)
    {
        lock (_lock)
        {
            return _generation == generation;
        }
    }

    /// <summary>
    ///     Adds the assigned partitions to the ones to catch up with, unless the consumer is caught up already.
    /// </summary>
    /// <param name="partitions">
    ///     The assigned partitions.
    /// </param>
    /// <returns>
    ///     The notification to be sent if the consumer just caught up, otherwise <c>null</c>.
    /// </returns>
    public CaughtUpNotification? AddPartitions(IEnumerable<TopicPartition> partitions)
    {
        if (!IsEnabled)
            return null;

        lock (_lock)
        {
            if (_isCaughtUp)
                return null;

            _isAssigned = true;

            foreach (TopicPartition partition in partitions)
            {
                _partitions.TryAdd(partition, new PartitionState());
            }

            return CheckCaughtUpCore();
        }
    }

    /// <summary>
    ///     Removes the revoked partitions from the ones to catch up with.
    /// </summary>
    /// <param name="partitions">
    ///     The revoked partitions.
    /// </param>
    /// <param name="checkCaughtUp">
    ///     Whether the remaining partitions can make the consumer caught up (i.e. it isn't stopping or disconnecting).
    /// </param>
    /// <returns>
    ///     The notification to be sent if the consumer just caught up, otherwise <c>null</c>.
    /// </returns>
    public CaughtUpNotification? RemovePartitions(IEnumerable<TopicPartition> partitions, bool checkCaughtUp)
    {
        if (!IsEnabled)
            return null;

        lock (_lock)
        {
            if (_isCaughtUp)
                return null;

            foreach (TopicPartition partition in partitions)
            {
                _partitions.Remove(partition);
            }

            return checkCaughtUp ? CheckCaughtUpCore() : null;
        }
    }

    /// <summary>
    ///     Tracks the record polled from a partition that is catching up, until its end is polled.
    /// </summary>
    /// <param name="topicPartitionOffset">
    ///     The offset of the polled record.
    /// </param>
    public void OnRecordPolled(TopicPartitionOffset topicPartitionOffset)
    {
        if (!IsEnabled)
            return;

        lock (_lock)
        {
            if (!TryGetCatchingUpPartitionCore(topicPartitionOffset.TopicPartition, out PartitionState? state) || state.IsEndReached)
                return;

            long offset = topicPartitionOffset.Offset.Value;

            if (state.LastPolledOffset == null || offset > state.LastPolledOffset)
                state.LastPolledOffset = offset;
        }
    }

    /// <summary>
    ///     Tracks the end of the partition.
    /// </summary>
    /// <param name="topicPartition">
    ///     The partition.
    /// </param>
    /// <returns>
    ///     Whether it's the first time the end of the partition is reached while catching up, and the notification to be sent if the
    ///     consumer just caught up.
    /// </returns>
    public (bool IsEndReached, CaughtUpNotification? Notification) OnEndOfPartitionPolled(TopicPartition topicPartition)
    {
        if (!IsEnabled)
            return (false, null);

        lock (_lock)
        {
            if (!TryGetCatchingUpPartitionCore(topicPartition, out PartitionState? state) || state.IsEndReached)
                return (false, null);

            state.IsEndReached = true;

            return (true, CheckCaughtUpCore());
        }
    }

    /// <summary>
    ///     Checks whether the partition is caught up, after its offsets were committed or moved (e.g. sought).
    /// </summary>
    /// <param name="topicPartition">
    ///     The partition.
    /// </param>
    /// <returns>
    ///     The notification to be sent if the consumer just caught up, otherwise <c>null</c>.
    /// </returns>
    public CaughtUpNotification? CheckPartition(TopicPartition topicPartition)
    {
        if (!IsEnabled)
            return null;

        lock (_lock)
        {
            if (!TryGetCatchingUpPartitionCore(topicPartition, out PartitionState? state) || !state.IsEndReached)
                return null;

            return CheckCaughtUpCore();
        }
    }

    /// <summary>
    ///     Tracks the record just read (or the end of the partition) and returns a value indicating whether it's the last one before
    ///     the end of a partition that isn't caught up yet, whose pending sequences must therefore be completed.
    /// </summary>
    /// <remarks>
    ///     The end of the partition counts only if the same channel instance read the last record before it: the end may otherwise be
    ///     forwarded to a channel replacing a stopped one, ahead of the records consumed again after the restart.
    /// </remarks>
    /// <param name="topicPartition">
    ///     The partition.
    /// </param>
    /// <param name="channelInstanceId">
    ///     The instance identifier of the channel that read the record.
    /// </param>
    /// <param name="offset">
    ///     The offset of the record just read, or <c>null</c> if the end of the partition was read.
    /// </param>
    /// <returns>
    ///     <c>true</c> if it's the last record before the end, otherwise <c>false</c>.
    /// </returns>
    public bool IsLastRecordBeforeEnd(TopicPartition topicPartition, Guid channelInstanceId, Offset? offset)
    {
        if (!IsEnabled)
            return false;

        lock (_lock)
        {
            if (!TryGetCatchingUpPartitionCore(topicPartition, out PartitionState? state))
                return false;

            if (offset != null)
            {
                state.LastReadChannelInstanceId = channelInstanceId;
                state.LastReadOffset = offset.Value.Value;
            }

            if (!state.IsEndReached)
                return false;

            return offset == null
                ? state.LastReadChannelInstanceId == channelInstanceId && state.LastReadOffset >= state.LastPolledOffset
                : offset.Value.Value == state.LastPolledOffset;
        }
    }

    /// <summary>
    ///     Returns a value indicating whether the partition doesn't have to be consumed any further to catch up, because it's caught up
    ///     or it isn't one of the partitions to catch up with.
    /// </summary>
    /// <param name="topicPartition">
    ///     The partition.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the partition doesn't have to be consumed any further, otherwise <c>false</c>.
    /// </returns>
    public bool IsPartitionCaughtUp(TopicPartition topicPartition)
    {
        if (!IsEnabled)
            return true;

        lock (_lock)
        {
            return !TryGetCatchingUpPartitionCore(topicPartition, out _);
        }
    }

    /// <summary>
    ///     Moves the end of the partition before the specified offset, because the records from there on won't be committed while
    ///     catching up: they belong to a sequence that doesn't complete at the end of the partition (e.g. an incomplete chunked
    ///     message), or they're committed only with a later batch (e.g. filtered out or unhandled at the end of a batch).
    /// </summary>
    /// <param name="topicPartition">
    ///     The partition.
    /// </param>
    /// <param name="offset">
    ///     The first offset that doesn't have to be committed to catch up.
    /// </param>
    /// <returns>
    ///     The notification to be sent if the consumer just caught up, otherwise <c>null</c>.
    /// </returns>
    public CaughtUpNotification? MoveEndBefore(TopicPartition topicPartition, long offset)
    {
        if (!IsEnabled)
            return null;

        lock (_lock)
        {
            if (!TryGetCatchingUpPartitionCore(topicPartition, out PartitionState? state) || !state.IsEndReached)
                return null;

            if (state.LastPolledOffset == null || offset - 1 < state.LastPolledOffset)
                state.LastPolledOffset = offset - 1;

            return CheckCaughtUpCore();
        }
    }

    /// <summary>
    ///     Marks the partition as caught up regardless of its commits, since its pending stream doesn't complete at the end of the
    ///     partition.
    /// </summary>
    /// <param name="topicPartition">
    ///     The partition.
    /// </param>
    /// <returns>
    ///     The notification to be sent if the consumer just caught up, otherwise <c>null</c>.
    /// </returns>
    public CaughtUpNotification? MarkCaughtUp(TopicPartition topicPartition)
    {
        if (!IsEnabled)
            return null;

        lock (_lock)
        {
            if (!TryGetCatchingUpPartitionCore(topicPartition, out PartitionState? state) || !state.IsEndReached)
                return null;

            state.IsCaughtUp = true;

            return CheckCaughtUpCore();
        }
    }

    public Task WaitUntilCaughtUpAsync(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return _caughtUpTaskCompletionSource.Task.WaitAsync(cancellationToken);
        }
    }

    // Must be called holding the lock
    private bool TryGetCatchingUpPartitionCore(TopicPartition topicPartition, [NotNullWhen(true)] out PartitionState? state)
    {
        if (!_isCaughtUp && _partitions.TryGetValue(topicPartition, out state) && !state.IsCaughtUp)
            return true;

        state = null;
        return false;
    }

    // Must be called holding the lock. Every partition is evaluated, so that a caught up partition isn't treated as still catching up
    // (e.g. completing its batches early) while another one is lagging.
    private CaughtUpNotification? CheckCaughtUpCore()
    {
        if (!_isAssigned || _partitions.Count == 0 && !_isStaticAssignment)
            return null;

        bool isCaughtUp = true;

        foreach ((TopicPartition topicPartition, PartitionState state) in _partitions)
        {
            if (state.IsCaughtUp)
                continue;

            if (state.IsEndReached && IsCommittedUntilEnd(topicPartition, state))
                state.IsCaughtUp = true;
            else
                isCaughtUp = false;
        }

        if (!isCaughtUp)
            return null;

        _isCaughtUp = true;

        return new CaughtUpNotification(_generation, _caughtUpTaskCompletionSource);
    }

    private bool IsCommittedUntilEnd(TopicPartition topicPartition, PartitionState state)
    {
        if (state.LastPolledOffset == null)
            return true;

        KafkaOffset? rollbackOffset = _offsetsTracker.GetRollbackOffset(topicPartition);

        // Sought to the end (see IKafkaConsumer.Seek): the records before it are skipped on purpose
        if (rollbackOffset?.Offset == Offset.End)
            return true;

        return rollbackOffset is { IsSpecial: false } && rollbackOffset.Offset.Value > state.LastPolledOffset;
    }

    public sealed record CaughtUpNotification(int Generation, TaskCompletionSource TaskCompletionSource);

    private sealed class PartitionState
    {
        public bool IsEndReached { get; set; }

        public bool IsCaughtUp { get; set; }

        public long? LastPolledOffset { get; set; }

        public Guid? LastReadChannelInstanceId { get; set; }

        public long? LastReadOffset { get; set; }
    }
}
