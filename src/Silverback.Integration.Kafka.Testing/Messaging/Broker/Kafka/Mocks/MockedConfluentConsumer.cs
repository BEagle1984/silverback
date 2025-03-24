// Copyright (c) 2025 Sergio Aquilini
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

    private readonly MockedConsumerGroup _consumerGroup;

    private readonly IMockedKafkaOptions _options;

    private readonly Dictionary<TopicPartition, TopicPartitionOffset> _currentOffsets = [];

    private readonly Dictionary<TopicPartition, TopicPartitionOffset> _storedOffsets = [];

    private readonly Dictionary<TopicPartition, Offset> _lastEofOffsets = [];

    private readonly int _autoCommitIntervalMs;

    private readonly List<TopicPartition> _pausedPartitions = [];

    public MockedConfluentConsumer(
        ConsumerConfig config,
        IInMemoryTopicCollection topics,
        IMockedConsumerGroupsCollection consumerGroups,
        IMockedKafkaOptions options)
    {
        _topics = Check.NotNull(topics, nameof(topics));
        _options = Check.NotNull(options, nameof(options));

        Name = $"{config.ClientId ?? "mocked"}.{Guid.NewGuid():N}";
        MemberId = Guid.NewGuid().ToString("N");
        Config = Check.NotNull(config, nameof(config));

        Check.NotNull(consumerGroups, nameof(consumerGroups));

        _consumerGroup = (MockedConsumerGroup)(!string.IsNullOrEmpty(Config.GroupId)
            ? consumerGroups.Get(Config)
            : consumerGroups.Get(Guid.NewGuid().ToString(), config.BootstrapServers));

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

    public bool PartitionsAssigned { get; private set; }

    public bool IsDisposed { get; private set; }

    internal Action<IConsumer<byte[]?, byte[]?>, string>? StatisticsHandler { get; set; }

    internal Action<IConsumer<byte[]?, byte[]?>, Error>? ErrorHandler { get; set; }

    internal Func<IConsumer<byte[]?, byte[]?>, List<TopicPartition>, IEnumerable<TopicPartitionOffset>>? PartitionsAssignedHandler { get; set; }

    internal Func<IConsumer<byte[]?, byte[]?>, List<TopicPartitionOffset>, IEnumerable<TopicPartitionOffset>>? PartitionsRevokedHandler { get; set; }

    internal Action<IConsumer<byte[]?, byte[]?>, CommittedOffsets>? OffsetsCommittedHandler { get; set; }

    public int AddBrokers(string brokers) => throw new NotSupportedException();

    public ConsumeResult<byte[]?, byte[]?> Consume(int millisecondsTimeout) =>
        throw new NotSupportedException();

    public ConsumeResult<byte[]?, byte[]?> Consume(CancellationToken cancellationToken = default)
    {
        ConsumeResult<byte[]?, byte[]?>? result;

        while (!TryConsume(cancellationToken, out result))
        {
            Thread.Sleep(10);
        }

        return result;
    }

    public ConsumeResult<byte[]?, byte[]?> Consume(TimeSpan timeout) => throw new NotSupportedException();

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
        Assign(new[] { new TopicPartitionOffset(partition, Offset.Unset) });

    public void Assign(TopicPartitionOffset partition) => Assign([partition]);

    public void Assign(IEnumerable<TopicPartitionOffset> partitions)
    {
        Assignment.Clear();

        IReadOnlyList<TopicPartitionOffset> partitionsList = Check.NotNull(partitions, nameof(partitions)).AsReadOnlyList();

        foreach (TopicPartitionOffset topicPartitionOffset in partitionsList)
        {
            Assignment.Add(topicPartitionOffset.TopicPartition);
            Seek(GetStartingOffset(topicPartitionOffset));
        }

        _consumerGroup.Assign(this, partitionsList.Select(partition => partition.TopicPartition));

        PartitionsAssigned = true;
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
        Assignment.Clear();
        _consumerGroup.Unassign(this);

        PartitionsAssigned = false;
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
        timestampsToSearch.Select(partitionTimestamp => new TopicPartitionOffset(partitionTimestamp.TopicPartition, Offset.End)).ToList();

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

    internal void OnRebalancing() => PartitionsAssigned = false;

    internal void OnPartitionsRevoked(IReadOnlyCollection<TopicPartition> topicPartitions)
    {
        PartitionsRevokedHandler?.Invoke(
            this,
            topicPartitions.Select(
                    topicPartition => _currentOffsets.GetValueOrDefault(
                        topicPartition,
                        new TopicPartitionOffset(topicPartition, Offset.Unset)))
                .ToList());

        if (Config.EnableAutoCommit != false && !string.IsNullOrEmpty(Config.GroupId))
            Commit();

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

    private bool TryConsume(CancellationToken cancellationToken, [NotNullWhen(true)] out ConsumeResult<byte[]?, byte[]?>? result)
    {
        Check.ThrowObjectDisposedIf(IsDisposed, GetType());

        cancellationToken.ThrowIfCancellationRequested();

        EnsurePartitionsAssigned(cancellationToken);

        // Process the assigned partitions starting from the one that consumed less messages
        IOrderedEnumerable<TopicPartitionOffset> topicPartitionsOffsets =
            _currentOffsets.Values
                .Where(
                    topicPartitionOffset => !IsPaused(
                        topicPartitionOffset.Topic,
                        topicPartitionOffset.Partition))
                .OrderBy(topicPartitionOffset => (int)topicPartitionOffset.Offset);

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
        return false;
    }

    private void EnsurePartitionsAssigned(CancellationToken cancellationToken)
    {
        if (PartitionsAssigned)
            return;

        while (_consumerGroup.IsRebalancing || _consumerGroup.IsRebalanceScheduled)
        {
            Task.Delay(10, cancellationToken).SafeWait(cancellationToken);
        }

        if (_options.PartitionsAssignmentDelay > TimeSpan.Zero)
            Task.Delay(_options.PartitionsAssignmentDelay, cancellationToken).SafeWait(cancellationToken);

        IReadOnlyCollection<TopicPartition> assignedPartitions = _consumerGroup
            .GetAssignment(this)
            .Where(topicPartition => !Assignment.Contains(topicPartition))
            .ToList();

        List<TopicPartitionOffset> partitionOffsets =
            PartitionsAssignedHandler?.Invoke(this, assignedPartitions.AsList()).ToList() ??
            assignedPartitions
                .Select(topicPartition => new TopicPartitionOffset(topicPartition, Offset.Unset))
                .ToList();

        foreach (TopicPartitionOffset partitionOffset in partitionOffsets)
        {
            Assignment.Add(partitionOffset.TopicPartition);
            Seek(GetStartingOffset(partitionOffset));
        }

        PartitionsAssigned = true;

        _consumerGroup.NotifyAssignmentComplete(this);
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

        // If the partition is empty the first offset would be Offset.Unset
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
                List<TopicPartitionOffsetError> topicPartitionOffsetErrors = committedOffsets
                    .Select(
                        topicPartitionOffset =>
                            new TopicPartitionOffsetError(topicPartitionOffset, null))
                    .ToList();

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
}
