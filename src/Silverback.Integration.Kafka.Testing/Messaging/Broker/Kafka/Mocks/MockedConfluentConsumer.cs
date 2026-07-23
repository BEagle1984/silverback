// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka.Mocks;

internal sealed class MockedConfluentConsumer : IMockedConfluentConsumer
{
    private readonly IInMemoryTopicCollection _topics;

    private readonly IInternalMockedConsumerGroup _consumerGroup;

    private readonly IMockedKafkaOptions _options;

    private readonly System.Threading.Lock _assignmentStateLock = new();

    private readonly Dictionary<TopicPartition, TopicPartitionOffset> _currentOffsets = [];

    private readonly Dictionary<TopicPartition, TopicPartitionOffset> _storedOffsets = [];

    private readonly Dictionary<TopicPartition, Offset> _lastEofOffsets = [];

    private readonly int _autoCommitIntervalMs;

    private readonly List<TopicPartition> _pausedPartitions = [];

    private readonly HashSet<TopicPartition> _pendingRevocations = [];

    private IReadOnlyCollection<TopicPartitionOffset>? _pendingAssignment;

    private long _rebalanceGeneration;

    private long _pendingAssignmentGeneration;

    private bool _partitionsAssigned;

    private bool _assignmentInProgress;

    private bool _revocationInProgress;

    private bool _revocationPending;

    public MockedConfluentConsumer(
        ConsumerConfig config,
        IInMemoryTopicCollection topics,
        IMockedConsumerGroupsCollection consumerGroups,
        IMockedKafkaOptions options)
        : this(
            config,
            topics,
            GetConsumerGroup(config, consumerGroups),
            options)
    {
    }

    internal MockedConfluentConsumer(
        ConsumerConfig config,
        IInMemoryTopicCollection topics,
        IInternalMockedConsumerGroup consumerGroup,
        IMockedKafkaOptions options)
    {
        _topics = Check.NotNull(topics, nameof(topics));
        _options = Check.NotNull(options, nameof(options));
        _consumerGroup = Check.NotNull(consumerGroup, nameof(consumerGroup));

        Name = $"{config.ClientId ?? "mocked"}.{Guid.NewGuid():N}";
        MemberId = Guid.NewGuid().ToString("N");
        Config = Check.NotNull(config, nameof(config));

        ConsumerGroupMetadata = new MockedConsumerGroupMetadata(_consumerGroup);

        if (config.EnableAutoCommit ?? true)
        {
            _autoCommitIntervalMs = options.OverriddenAutoCommitIntervalMs ??
                                    config.AutoCommitIntervalMs ??
                                    5000;

            Task.Run(AutoCommitAsync).FireAndForget();
        }
    }

    public Handle Handle => throw new NotSupportedException();

    public string Name { get; }

    public string MemberId { get; }

    public ConsumerConfig Config { get; }

    public List<TopicPartition> Assignment { get; } = [];

    public List<string> Subscription { get; } = [];

    public IConsumerGroupMetadata ConsumerGroupMetadata { get; }

    public bool PartitionsAssigned
    {
        get
        {
            lock (_assignmentStateLock)
            {
                return _partitionsAssigned;
            }
        }
    }

    public bool IsDisposed { get; private set; }

    internal Action<IConsumer<byte[]?, byte[]?>, string>? StatisticsHandler { get; set; }

    internal Action<IConsumer<byte[]?, byte[]?>, Error>? ErrorHandler { get; set; }

    internal Func<IConsumer<byte[]?, byte[]?>, List<TopicPartition>, IEnumerable<TopicPartitionOffset>>? PartitionsAssignedHandler { get; set; }

    internal Func<IConsumer<byte[]?, byte[]?>, List<TopicPartitionOffset>, IEnumerable<TopicPartitionOffset>>? PartitionsRevokedHandler { get; set; }

    internal Action<IConsumer<byte[]?, byte[]?>, CommittedOffsets>? OffsetsCommittedHandler { get; set; }

    public int AddBrokers(string brokers) => throw new NotSupportedException();

    public ConsumeResult<byte[]?, byte[]?>? Consume(int millisecondsTimeout) => Consume(TimeSpan.FromMilliseconds(millisecondsTimeout));

    public ConsumeResult<byte[]?, byte[]?> Consume(CancellationToken cancellationToken = default)
    {
        ConsumeResult<byte[]?, byte[]?>? result;

        while (!TryConsume(cancellationToken, out result) || result == null)
        {
            Thread.Sleep(10);
        }

        return result;
    }

    public ConsumeResult<byte[]?, byte[]?>? Consume(TimeSpan timeout)
    {
        DateTime startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < timeout)
        {
            // A cancellation token is still needed to avoid blocking too long for cases when the partition assignment is delayed
            // by a lot (health check tests, for example)
            using CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromMilliseconds(100));

            try
            {
                if (TryConsume(cancellationTokenSource.Token, out ConsumeResult<byte[]?, byte[]?>? result))
                {
                    // Only return a result if it is not null, otherwise continue trying to consume
                    if (result != null)
                        return result;
                }
                else
                {
                    // Break early if the partitions are not assigned yet (e.g., during rebalancing)
                    return null;
                }
            }
            catch (OperationCanceledException)
            {
                // Swallow the exception since the caller doesn't expect it from this method
                return null;
            }

            Thread.Sleep(10);
        }

        return null;
    }

    public void Subscribe(IEnumerable<string> topics)
    {
        Check.ThrowObjectDisposedIf(IsDisposed, GetType());
        CheckGroupIdNotEmpty();

        IReadOnlyList<string> topicsList = Check.NotNull(topics, nameof(topics)).AsReadOnlyList();
        Check.NotEmpty(topicsList, nameof(topics));
        Check.HasNoEmpties(topicsList, nameof(topics));

        Subscription.Clear();
        Subscription.AddRange(topicsList);
        _consumerGroup.Subscribe(this, topicsList);
    }

    public void Subscribe(string topic) => Subscribe([topic]);

    public void Unsubscribe()
    {
        Check.ThrowObjectDisposedIf(IsDisposed, GetType());
        CheckGroupIdNotEmpty();

        _consumerGroup.Unsubscribe(this);
        Subscription.Clear();
    }

    public void Assign(TopicPartition partition) =>
        Assign([new TopicPartitionOffset(partition, Offset.Unset)]);

    public void Assign(TopicPartitionOffset partition) => Assign([partition]);

    public void Assign(IEnumerable<TopicPartitionOffset> partitions)
    {
        IReadOnlyList<TopicPartitionOffset> partitionsList = Check.NotNull(partitions, nameof(partitions)).AsReadOnlyList();
        long assignmentGeneration;

        lock (_assignmentStateLock)
        {
            _rebalanceGeneration++;
            assignmentGeneration = _rebalanceGeneration;
            _pendingAssignment = null;
            _partitionsAssigned = false;
            Assignment.Clear();

            foreach (TopicPartitionOffset topicPartitionOffset in partitionsList)
            {
                Assignment.Add(topicPartitionOffset.TopicPartition);
                Seek(GetStartingOffset(topicPartitionOffset));
            }
        }

        _consumerGroup.Assign(this, partitionsList.Select(partition => partition.TopicPartition));

        lock (_assignmentStateLock)
        {
            if (assignmentGeneration == _rebalanceGeneration &&
                Assignment.ToHashSet().SetEquals(_consumerGroup.GetAssignment(this)))
            {
                _partitionsAssigned = true;
            }
        }
    }

    public void Assign(IEnumerable<TopicPartition> partitions) =>
        Assign(partitions.Select(partition => new TopicPartitionOffset(partition, Offset.Unset)));

    public void IncrementalAssign(IEnumerable<TopicPartitionOffset> partitions) =>
        throw new NotSupportedException();

    public void IncrementalAssign(IEnumerable<TopicPartition> partitions) =>
        throw new NotSupportedException();

    public void IncrementalUnassign(IEnumerable<TopicPartition> partitions) =>
        throw new NotSupportedException();

    public void Unassign()
    {
        lock (_assignmentStateLock)
        {
            _pendingAssignment = null;
            Assignment.Clear();
            _partitionsAssigned = false;
            _rebalanceGeneration++;
        }

        _consumerGroup.Unassign(this);
    }

    public void StoreOffset(ConsumeResult<byte[]?, byte[]?> result)
    {
        Check.NotNull(result, nameof(result));

        Check.ThrowObjectDisposedIf(IsDisposed, GetType());

        StoreOffset(result.TopicPartitionOffset);
    }

    public void StoreOffset(TopicPartitionOffset offset)
    {
        Check.ThrowObjectDisposedIf(IsDisposed, GetType());

        Check.NotNull(offset, nameof(offset));

        lock (_storedOffsets)
        {
            _storedOffsets[offset.TopicPartition] = offset;
        }
    }

    public List<TopicPartitionOffset> Commit() => CommitCore(false);

    public void Commit(IEnumerable<TopicPartitionOffset> offsets) => throw new NotSupportedException();

    public void Commit(ConsumeResult<byte[]?, byte[]?> result) => throw new NotSupportedException();

    public void Seek(TopicPartitionOffset tpo)
    {
        Check.NotNull(tpo, nameof(tpo));

        _currentOffsets[tpo.TopicPartition] = tpo;
    }

    public void Pause(IEnumerable<TopicPartition> partitions)
    {
        lock (_pausedPartitions)
        {
            partitions
                .Where(partition => !_pausedPartitions.Contains(partition))
                .ForEach(_pausedPartitions.Add);
        }
    }

    [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "False positive")]
    public void Resume(IEnumerable<TopicPartition> partitions)
    {
        lock (_pausedPartitions)
        {
            partitions
                .Where(_pausedPartitions.Contains)
                .ForEach(partition => _pausedPartitions.Remove(partition));
        }
    }

    public List<TopicPartitionOffset> Committed(TimeSpan timeout) => throw new NotSupportedException();

    public List<TopicPartitionOffset> Committed(
        IEnumerable<TopicPartition> partitions,
        TimeSpan timeout) =>
        throw new NotSupportedException();

    public Offset Position(TopicPartition partition) => throw new NotSupportedException();

    public List<TopicPartitionOffset> OffsetsForTimes(IEnumerable<TopicPartitionTimestamp> timestampsToSearch, TimeSpan timeout) =>
        [.. timestampsToSearch.Select(partitionTimestamp => new TopicPartitionOffset(partitionTimestamp.TopicPartition, Offset.End))];

    public WatermarkOffsets GetWatermarkOffsets(TopicPartition topicPartition) =>
        throw new NotSupportedException();

    public WatermarkOffsets QueryWatermarkOffsets(TopicPartition topicPartition, TimeSpan timeout) =>
        throw new NotSupportedException();

    public TopicPartitionOffset GetStoredOffset(TopicPartition topicPartition)
    {
        lock (_storedOffsets)
        {
            return _storedOffsets.TryGetValue(topicPartition, out TopicPartitionOffset? topicPartitionOffset)
                ? topicPartitionOffset
                : new TopicPartitionOffset(topicPartition, Offset.Unset);
        }
    }

    public void Close() => _consumerGroup.Remove(this);

    public void SetSaslCredentials(string username, string password) => throw new NotSupportedException();

    public void Dispose()
    {
        Close();
        IsDisposed = true;
    }

    internal void OnRebalancing()
    {
        lock (_assignmentStateLock)
        {
            _rebalanceGeneration++;
            _pendingAssignment = null;
            _partitionsAssigned = false;
        }
    }

    internal void OnPartitionsRevoked(IReadOnlyCollection<TopicPartition> topicPartitions)
    {
        IReadOnlyCollection<TopicPartition>? partitionsToRevoke;

        lock (_assignmentStateLock)
        {
            // The assignment handler updates the outer KafkaConsumer before it returns. Defer revocation while that
            // handler is running to preserve the assigned-then-revoked callback order and final Connected status.
            QueueRevocation(topicPartitions);

            if (!TryStartRevocation(out partitionsToRevoke))
                return;
        }

        ProcessRevocations(partitionsToRevoke);
    }

    internal bool EnsurePartitionsAssigned(CancellationToken cancellationToken)
    {
        long assignmentGeneration;

        lock (_assignmentStateLock)
        {
            if (_partitionsAssigned)
                return true;

            if (_assignmentInProgress || _revocationInProgress ||
                _consumerGroup.IsRebalancing || _consumerGroup.IsRebalanceScheduled)
            {
                return false;
            }

            if (TryCommitPendingAssignment())
                return true;

            _assignmentInProgress = true;
            assignmentGeneration = _rebalanceGeneration;
        }

        bool assignmentPassCompleted = false;

        try
        {
            if (_options.PartitionsAssignmentDelay > TimeSpan.Zero)
                Task.Delay(_options.PartitionsAssignmentDelay, cancellationToken).SafeWait();

            IReadOnlyCollection<TopicPartition> assignedPartitions =
            [
                .. _consumerGroup
                    .GetAssignment(this)
                    .Where(topicPartition => !Assignment.Contains(topicPartition))
            ];

            // Never hold the assignment-state lock across callbacks. A concurrent or reentrant revocation is queued
            // until this handler returns, and the generation check below prevents this pass from publishing afterward.
            List<TopicPartitionOffset> partitionOffsets =
                PartitionsAssignedHandler?.Invoke(this, assignedPartitions.AsList()).ToList() ??
                [.. assignedPartitions.Select(topicPartition => new TopicPartitionOffset(topicPartition, Offset.Unset))];

            bool assignmentCommitted = CompleteAssignment(assignmentGeneration, partitionOffsets);
            assignmentPassCompleted = true;
            return assignmentCommitted;
        }
        finally
        {
            if (!assignmentPassCompleted)
                AbortAssignment();
        }
    }

    private void RevokePartitions(IReadOnlyCollection<TopicPartition> topicPartitions)
    {
        PartitionsRevokedHandler?.Invoke(
            this,
            [
                .. topicPartitions.Select(topicPartition => _currentOffsets.GetValueOrDefault(
                    topicPartition,
                    new TopicPartitionOffset(topicPartition, Offset.Unset)))
            ]);

        if (Config.EnableAutoCommit != false && !string.IsNullOrEmpty(Config.GroupId))
            Commit();

        lock (_assignmentStateLock)
        {
            foreach (TopicPartition topicPartition in topicPartitions)
            {
                Assignment.Remove(topicPartition);
                _currentOffsets.Remove(topicPartition);

                lock (_storedOffsets)
                {
                    _storedOffsets.Remove(topicPartition);
                }

                _lastEofOffsets.Remove(topicPartition);
            }
        }
    }

    private bool TryConsume(CancellationToken cancellationToken, out ConsumeResult<byte[]?, byte[]?>? result)
    {
        Check.ThrowObjectDisposedIf(IsDisposed, GetType());

        cancellationToken.ThrowIfCancellationRequested();

        if (!EnsurePartitionsAssigned(cancellationToken))
        {
            result = null;
            return false;
        }

        // Process the assigned partitions starting from the one that consumed fewer messages
        IOrderedEnumerable<TopicPartitionOffset> topicPartitionsOffsets =
            _currentOffsets.Values
                .Where(topicPartitionOffset => !IsPaused(
                    topicPartitionOffset.Topic,
                    topicPartitionOffset.Partition))
                .OrderBy(topicPartitionOffset => (int)(long)topicPartitionOffset.Offset);

        cancellationToken.ThrowIfCancellationRequested();

        foreach (TopicPartitionOffset topicPartitionOffset in topicPartitionsOffsets)
        {
            IInMemoryPartition inMemoryPartition = _topics.Get(topicPartitionOffset.Topic, Config).Partitions[topicPartitionOffset.Partition];

            bool pulled = inMemoryPartition.TryPull(topicPartitionOffset.Offset, out result);

            cancellationToken.ThrowIfCancellationRequested();

            if (!Assignment.Contains(topicPartitionOffset.TopicPartition))
                continue;

            if (pulled)
            {
                _currentOffsets[result!.TopicPartition] = new TopicPartitionOffset(result.TopicPartition, result.Offset + 1);
                return true;
            }

            if (Config.EnablePartitionEof == true && GetEofMessageIfNeeded(topicPartitionOffset, out result))
                return true;
        }

        result = null;
        return true;
    }

    private bool CompleteAssignment(
        long assignmentGeneration,
        IReadOnlyCollection<TopicPartitionOffset> partitionOffsets)
    {
        bool assignmentCommitted;
        IReadOnlyCollection<TopicPartition>? partitionsToRevoke;

        lock (_assignmentStateLock)
        {
            IReadOnlyCollection<TopicPartition> groupAssignment = _consumerGroup.GetAssignment(this);
            HashSet<TopicPartition> resultingAssignment =
            [
                .. Assignment,
                .. partitionOffsets.Select(partitionOffset => partitionOffset.TopicPartition)
            ];
            bool assignmentMatches = resultingAssignment.SetEquals(groupAssignment);

            assignmentCommitted =
                assignmentGeneration == _rebalanceGeneration &&
                !_consumerGroup.IsRebalancing &&
                !_consumerGroup.IsRebalanceScheduled &&
                assignmentMatches;

            if (assignmentCommitted)
            {
                CommitAssignment(partitionOffsets);
            }
            else if (assignmentGeneration != _rebalanceGeneration &&
                     !_revocationPending &&
                     assignmentMatches)
            {
                // No partition was revoked and the assignment is unchanged. Preserve the already invoked callback
                // result, but only publish it from a subsequent pass after revalidating the current generation.
                _pendingAssignment = [.. partitionOffsets];
                _pendingAssignmentGeneration = _rebalanceGeneration;
            }

            _assignmentInProgress = false;
            TryStartRevocation(out partitionsToRevoke);
        }

        if (partitionsToRevoke != null)
            ProcessRevocations(partitionsToRevoke);

        return assignmentCommitted;
    }

    private void AbortAssignment()
    {
        IReadOnlyCollection<TopicPartition>? partitionsToRevoke;

        lock (_assignmentStateLock)
        {
            _assignmentInProgress = false;
            TryStartRevocation(out partitionsToRevoke);
        }

        if (partitionsToRevoke != null)
            ProcessRevocations(partitionsToRevoke);
    }

    private void QueueRevocation(IEnumerable<TopicPartition> topicPartitions)
    {
        _pendingAssignment = null;
        _revocationPending = true;
        _pendingRevocations.UnionWith(topicPartitions);
    }

    private bool TryCommitPendingAssignment()
    {
        if (_pendingAssignment == null)
            return false;

        IReadOnlyCollection<TopicPartitionOffset> pendingAssignment = _pendingAssignment;
        _pendingAssignment = null;

        if (_pendingAssignmentGeneration != _rebalanceGeneration)
            return false;

        HashSet<TopicPartition> resultingAssignment =
        [
            .. Assignment,
            .. pendingAssignment.Select(partitionOffset => partitionOffset.TopicPartition)
        ];

        if (!resultingAssignment.SetEquals(_consumerGroup.GetAssignment(this)))
            return false;

        CommitAssignment(pendingAssignment);
        return true;
    }

    private void CommitAssignment(IEnumerable<TopicPartitionOffset> partitionOffsets)
    {
        foreach (TopicPartitionOffset partitionOffset in partitionOffsets)
        {
            if (!Assignment.Contains(partitionOffset.TopicPartition))
                Assignment.Add(partitionOffset.TopicPartition);

            Seek(GetStartingOffset(partitionOffset));
        }

        _partitionsAssigned = true;
        _consumerGroup.NotifyAssignmentComplete(this);
    }

    private bool TryStartRevocation([NotNullWhen(true)] out IReadOnlyCollection<TopicPartition>? topicPartitions)
    {
        if (!_revocationPending || _assignmentInProgress || _revocationInProgress)
        {
            topicPartitions = null;
            return false;
        }

        topicPartitions = [.. _pendingRevocations];
        _pendingRevocations.Clear();
        _revocationPending = false;
        _revocationInProgress = true;

        return true;
    }

    private void ProcessRevocations(IReadOnlyCollection<TopicPartition> topicPartitions)
    {
        IReadOnlyCollection<TopicPartition>? nextRevocation = topicPartitions;

        while (nextRevocation != null)
        {
            try
            {
                RevokePartitions(nextRevocation);
            }
            finally
            {
                lock (_assignmentStateLock)
                {
                    _revocationInProgress = false;
                    TryStartRevocation(out nextRevocation);
                }
            }
        }
    }

    private TopicPartitionOffset GetStartingOffset(TopicPartitionOffset topicPartitionOffset)
    {
        if (!topicPartitionOffset.Offset.IsSpecial)
            return topicPartitionOffset;

        Offset offset = topicPartitionOffset.Offset;

        if (offset == Offset.Stored || offset == Offset.Unset)
            offset = _consumerGroup.GetCommittedOffset(topicPartitionOffset.TopicPartition)?.Offset ?? Offset.Unset;

        IInMemoryTopic topic = _topics.Get(topicPartitionOffset.Topic, Config);

        if (offset == Offset.End)
            offset = topic.GetLastOffset(topicPartitionOffset.Partition);
        else if (offset.IsSpecial)
            offset = topic.GetFirstOffset(topicPartitionOffset.Partition);

        // If the partition is empty, the first offset would be Offset.Unset
        if (offset.IsSpecial)
            offset = new Offset(0);

        return new TopicPartitionOffset(topicPartitionOffset.TopicPartition, offset);
    }

    private bool IsPaused(string topic, Partition partition)
    {
        lock (_pausedPartitions)
        {
            return _pausedPartitions.Contains(new TopicPartition(topic, partition));
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Ensures retry in next iteration")]
    private async Task AutoCommitAsync()
    {
        while (!IsDisposed)
        {
            try
            {
                CommitCore(true);
            }
            catch (Exception)
            {
                // Ignore
            }

            await Task.Delay(_autoCommitIntervalMs).ConfigureAwait(false);
        }
    }

    private List<TopicPartitionOffset> CommitCore(bool isAutoCommit)
    {
        Check.ThrowObjectDisposedIf(IsDisposed, GetType());
        CheckGroupIdNotEmpty();

        lock (_storedOffsets)
        {
            if (_storedOffsets.Count == 0)
                return [];

            List<TopicPartitionOffset> committedOffsets = [.. _storedOffsets.Values];
            _consumerGroup.Commit(committedOffsets);
            _storedOffsets.Clear();

            if (isAutoCommit)
            {
                List<TopicPartitionOffsetError> topicPartitionOffsetErrors =
                [
                    .. committedOffsets
                        .Select(topicPartitionOffset =>
                            new TopicPartitionOffsetError(topicPartitionOffset, null))
                ];

                OffsetsCommittedHandler?.Invoke(this, new CommittedOffsets(topicPartitionOffsetErrors, null));
            }

            return committedOffsets;
        }
    }

    private bool GetEofMessageIfNeeded(
        TopicPartitionOffset topicPartitionOffset,
        [NotNullWhen(true)] out ConsumeResult<byte[]?, byte[]?>? result)
    {
        _lastEofOffsets.TryAdd(topicPartitionOffset.TopicPartition, -1);

        Offset lastEofOffset = _lastEofOffsets[topicPartitionOffset.TopicPartition];

        if (lastEofOffset < topicPartitionOffset.Offset)
        {
            _lastEofOffsets[topicPartitionOffset.TopicPartition] = topicPartitionOffset.Offset;

            result = new ConsumeResult<byte[]?, byte[]?>
            {
                IsPartitionEOF = true,
                Offset = topicPartitionOffset.Offset - 1,
                Topic = topicPartitionOffset.Topic,
                Partition = topicPartitionOffset.Partition
            };

            return true;
        }

        result = null;
        return false;
    }

    private void CheckGroupIdNotEmpty()
    {
        if (string.IsNullOrEmpty(Config.GroupId))
            throw new ArgumentException("'group.id' configuration parameter is required and was not specified.");
    }

    [SuppressMessage("Style", "SA1204:Static members should appear before non-static members", Justification = "Used by the delegating constructor.")]
    private static IInternalMockedConsumerGroup GetConsumerGroup(
        ConsumerConfig config,
        IMockedConsumerGroupsCollection consumerGroups)
    {
        Check.NotNull(config, nameof(config));
        Check.NotNull(consumerGroups, nameof(consumerGroups));

        IMockedConsumerGroup consumerGroup = !string.IsNullOrEmpty(config.GroupId)
            ? consumerGroups.Get(config)
            : consumerGroups.Get(Guid.NewGuid().ToString(), config.BootstrapServers);

        return (IInternalMockedConsumerGroup)consumerGroup;
    }
}
