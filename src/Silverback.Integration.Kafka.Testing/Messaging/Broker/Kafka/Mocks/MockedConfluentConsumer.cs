// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka.Mocks
{
    internal sealed class MockedConfluentConsumer : IMockedConfluentConsumer
    {
        private readonly ConsumerConfig _config;

        private readonly IInMemoryTopicCollection _topics;

        private readonly IMockedKafkaOptions _options;

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<Partition, Offset>> _currentOffsets
            = new();

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<Partition, Offset>> _storedOffsets
            = new();

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<Partition, Offset>>
            _lastPartitionEof
                = new();

        private readonly List<TopicPartitionOffset> _temporaryAssignment = new();

        private readonly List<string> _topicAssignments = new();

        private readonly int _autoCommitIntervalMs;

        private readonly List<TopicPartition> _pausedPartitions = new();

        [SuppressMessage("", "VSTHRD110", Justification = Justifications.FireAndForget)]
        public MockedConfluentConsumer(
            ConsumerConfig config,
            IInMemoryTopicCollection topics,
            IMockedKafkaOptions options)
        {
            _config = Check.NotNull(config, nameof(config));
            _topics = Check.NotNull(topics, nameof(topics));
            _options = Check.NotNull(options, nameof(options));

            Name = $"{config.ClientId ?? "mocked"}.{Guid.NewGuid():N}";
            GroupId = config.GroupId;
            MemberId = Guid.NewGuid().ToString("N");
            EnablePartitionEof = config.EnablePartitionEof ?? false;

            if (config.EnableAutoCommit ?? true)
            {
                _autoCommitIntervalMs = options.OverriddenAutoCommitIntervalMs ??
                                        config.AutoCommitIntervalMs ??
                                        5000;

                Task.Run(AutoCommitAsync);
            }
        }

        public Handle Handle => throw new NotSupportedException();

        public string Name { get; }

        public string MemberId { get; }

        public bool EnablePartitionEof { get; }

        public List<TopicPartition> Assignment { get; } = new();

        public List<string> Subscription { get; } = new();

        public IConsumerGroupMetadata ConsumerGroupMetadata => throw new NotSupportedException();

        public string GroupId { get; private set; }

        public bool PartitionsAssigned { get; private set; }

        public bool Disposed { get; private set; }

        internal Action<IConsumer<byte[]?, byte[]?>, string>? StatisticsHandler { get; set; }

        internal Action<IConsumer<byte[]?, byte[]?>, Error>? ErrorHandler { get; set; }

        internal Func<IConsumer<byte[]?, byte[]?>, List<TopicPartition>, IEnumerable<TopicPartitionOffset>>?
            PartitionsAssignedHandler { get; set; }

        internal Func<IConsumer<byte[]?, byte[]?>, List<TopicPartitionOffset>,
            IEnumerable<TopicPartitionOffset>>? PartitionsRevokedHandler { get; set; }

        internal Action<IConsumer<byte[]?, byte[]?>, CommittedOffsets>? OffsetsCommittedHandler { get; set; }

        public int AddBrokers(string brokers) => throw new NotSupportedException();

        public ConsumeResult<byte[]?, byte[]?> Consume(int millisecondsTimeout) =>
            throw new NotSupportedException();

        public ConsumeResult<byte[]?, byte[]?> Consume(CancellationToken cancellationToken = default)
        {
            while (true)
            {
                EnsureNotDisposed();

                if (TryConsume(cancellationToken, out var result))
                    return result!;

                Thread.Sleep(10);
            }
        }

        public ConsumeResult<byte[]?, byte[]?> Consume(TimeSpan timeout) => throw new NotSupportedException();

        public void Subscribe(IEnumerable<string> topics)
        {
            EnsureNotDisposed();

            if (string.IsNullOrEmpty(GroupId))
            {
                throw new ArgumentException(
                    "'group.id' configuration parameter is required and was not specified.");
            }

            var topicsList =
                Check.NotNull(topics, nameof(topics)).AsReadOnlyList();
            Check.NotEmpty(topicsList, nameof(topics));
            Check.HasNoEmpties(topicsList, nameof(topics));

            lock (Subscription)
            {
                Subscription.Clear();
                Subscription.AddRange(topicsList);
                Subscription.ForEach(topic => _topics.Get(topic, _config).Subscribe(this));
            }
        }

        public void Subscribe(string topic) => Subscribe(new[] { topic });

        public void Unsubscribe()
        {
            EnsureNotDisposed();

            lock (Subscription)
            {
                foreach (var topic in Subscription)
                {
                    _topics.Get(topic, _config).Unsubscribe(this);
                }

                Subscription.Clear();
            }
        }

        public void Assign(TopicPartition partition) => throw new NotSupportedException();

        public void Assign(TopicPartitionOffset partition) => throw new NotSupportedException();

        public void Assign(IEnumerable<TopicPartitionOffset> partitions)
        {
            if (string.IsNullOrEmpty(GroupId))
                GroupId = Guid.NewGuid().ToString();

            Assignment.Clear();

            foreach (var topicPartitionOffset in partitions)
            {
                Assignment.Add(topicPartitionOffset.TopicPartition);
                _topics.Get(topicPartitionOffset.Topic, _config).Assign(this, topicPartitionOffset.Partition);
                Seek(GetStartingOffset(topicPartitionOffset));
            }

            PartitionsAssigned = true;
        }

        public void Assign(IEnumerable<TopicPartition> partitions) => throw new NotSupportedException();

        public void IncrementalAssign(IEnumerable<TopicPartitionOffset> partitions) =>
            throw new NotSupportedException();

        public void IncrementalAssign(IEnumerable<TopicPartition> partitions) =>
            throw new NotSupportedException();

        public void IncrementalUnassign(IEnumerable<TopicPartition> partitions) =>
            throw new NotSupportedException();

        public void Unassign() => throw new NotSupportedException();

        public void StoreOffset(ConsumeResult<byte[]?, byte[]?> result)
        {
            EnsureNotDisposed();

            if (result == null)
                return;

            StoreOffset(result.TopicPartitionOffset);
        }

        public void StoreOffset(TopicPartitionOffset offset)
        {
            EnsureNotDisposed();

            if (offset == null)
                return;

            var partitionOffsetDictionary = _storedOffsets.GetOrAdd(
                offset.Topic,
                _ => new ConcurrentDictionary<Partition, Offset>());

            partitionOffsetDictionary[offset.Partition] = offset.Offset;
        }

        public List<TopicPartitionOffset> Commit()
        {
            EnsureNotDisposed();

            var topicPartitionOffsets = _storedOffsets.SelectMany(
                topicPair => topicPair.Value.Select(
                    partitionPair => new TopicPartitionOffset(
                        topicPair.Key,
                        partitionPair.Key,
                        partitionPair.Value))).ToList();

            var topicPartitionOffsetsByTopic =
                topicPartitionOffsets.GroupBy(topicPartitionOffset => topicPartitionOffset.Topic);

            var actualCommittedOffsets = new List<TopicPartitionOffset>();
            foreach (var group in topicPartitionOffsetsByTopic)
            {
                actualCommittedOffsets.AddRange(_topics.Get(group.Key, _config).Commit(GroupId, group));
            }

            if (actualCommittedOffsets.Count > 0)
            {
                OffsetsCommittedHandler?.Invoke(
                    this,
                    new CommittedOffsets(
                        actualCommittedOffsets
                            .Select(
                                topicPartitionOffset =>
                                    new TopicPartitionOffsetError(topicPartitionOffset, null)).ToList(),
                        null));
            }

            return actualCommittedOffsets;
        }

        public void Commit(IEnumerable<TopicPartitionOffset> offsets) => throw new NotSupportedException();

        public void Commit(ConsumeResult<byte[]?, byte[]?> result) => throw new NotSupportedException();

        public void Seek(TopicPartitionOffset tpo)
        {
            Check.NotNull(tpo, nameof(tpo));

            if (!_currentOffsets.ContainsKey(tpo.Topic))
                _currentOffsets[tpo.Topic] = new ConcurrentDictionary<Partition, Offset>();

            _currentOffsets[tpo.Topic][tpo.Partition] = tpo.Offset;
        }

        public void Pause(IEnumerable<TopicPartition> partitions)
        {
            lock (_pausedPartitions)
            {
                partitions
                    .Where(partition => !_pausedPartitions.Contains(partition))
                    .ForEach(partition => _pausedPartitions.Add(partition));
            }
        }

        public void Resume(IEnumerable<TopicPartition> partitions)
        {
            lock (_pausedPartitions)
            {
                partitions
                    .Where(partition => _pausedPartitions.Contains(partition))
                    .ForEach(partition => _pausedPartitions.Remove(partition));
            }
        }

        public List<TopicPartitionOffset> Committed(TimeSpan timeout) => throw new NotSupportedException();

        public List<TopicPartitionOffset> Committed(
            IEnumerable<TopicPartition> partitions,
            TimeSpan timeout) =>
            throw new NotSupportedException();

        public Offset Position(TopicPartition partition) => throw new NotSupportedException();

        public List<TopicPartitionOffset> OffsetsForTimes(
            IEnumerable<TopicPartitionTimestamp> timestampsToSearch,
            TimeSpan timeout) =>
            throw new NotSupportedException();

        public WatermarkOffsets GetWatermarkOffsets(TopicPartition topicPartition) =>
            throw new NotSupportedException();

        public WatermarkOffsets QueryWatermarkOffsets(TopicPartition topicPartition, TimeSpan timeout) =>
            throw new NotSupportedException();

        public void Close()
        {
            // Nothing to close, it's just a mock
        }

        public void Dispose() => Disposed = true;

        internal void OnRebalancing()
        {
            PartitionsAssigned = false;
        }

        internal void OnPartitionsRevoked(string topicName)
        {
            PartitionsAssigned = false;

            if (_topicAssignments.Contains(topicName))
                _topicAssignments.Remove(topicName);

            if (_topicAssignments.Count > 0)
                return;

            InvokePartitionsRevokedHandler(topicName);

            ClearPartitionsAssignment();
        }

        internal void OnPartitionsAssigned(string topicName, IReadOnlyCollection<Partition> partitions)
        {
            _temporaryAssignment.RemoveAll(topicPartitionOffset => topicPartitionOffset.Topic == topicName);
            _temporaryAssignment.AddRange(
                partitions.Select(partition => new TopicPartitionOffset(topicName, partition, Offset.Unset)));

            if (!_topicAssignments.Contains(topicName))
                _topicAssignments.Add(topicName);

            if (_topicAssignments.Count < Subscription.Count)
                return;

            var partitionOffsets =
                InvokePartitionsAssignedHandler(_temporaryAssignment) ?? _temporaryAssignment;

            foreach (var partitionOffset in partitionOffsets)
            {
                Assignment.Add(partitionOffset.TopicPartition);
                Seek(GetStartingOffset(partitionOffset));
            }

            PartitionsAssigned = true;
        }

        private List<TopicPartitionOffset>? InvokePartitionsAssignedHandler(
            IEnumerable<TopicPartitionOffset> partitionOffsets) =>
            PartitionsAssignedHandler?.Invoke(
                    this,
                    partitionOffsets.Select(partitionOffset => partitionOffset.TopicPartition).ToList())
                ?.ToList();

        private void InvokePartitionsRevokedHandler(string topicName)
        {
            if (PartitionsRevokedHandler == null || Assignment.Count == 0)
                return;

            PartitionsRevokedHandler.Invoke(
                this,
                Assignment.Where(topicPartition => topicPartition.Topic == topicName).Select(
                        partition => new TopicPartitionOffset(
                            partition,
                            _topics.Get(partition.Topic, _config).GetCommittedOffset(
                                partition.Partition,
                                GroupId)))
                    .ToList());
        }

        private void ClearPartitionsAssignment()
        {
            Assignment.ToList().ForEach(topicPartition => Assignment.Remove(topicPartition));

            _currentOffsets.Clear();
            _storedOffsets.Clear();
            _lastPartitionEof.Clear();
        }

        private bool TryConsume(
            CancellationToken cancellationToken,
            out ConsumeResult<byte[]?, byte[]?>? result)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Subscription.ForEach(
                topic => _topics.Get(topic, _config)
                    .EnsurePartitionsAssigned(
                        this,
                        _options.PartitionsAssignmentDelay,
                        cancellationToken));

            // Process the assigned partitions starting from the one that consumed less messages
            var topicPartitionsOffsets = _currentOffsets
                .SelectMany(
                    topicPair =>
                        topicPair.Value.Select(
                            partitionPair => new TopicPartitionOffset(
                                topicPair.Key,
                                partitionPair.Key,
                                partitionPair.Value)))
                .Where(
                    topicPartitionOffset => !IsPaused(
                        topicPartitionOffset.Topic,
                        topicPartitionOffset.Partition))
                .OrderBy(topicPartitionOffset => (int)topicPartitionOffset.Offset);

            cancellationToken.ThrowIfCancellationRequested();

            foreach (var topicPartitionOffset in topicPartitionsOffsets)
            {
                var inMemoryPartition =
                    _topics.Get(topicPartitionOffset.Topic, _config)
                        .Partitions[topicPartitionOffset.Partition];

                bool pulled = inMemoryPartition.TryPull(topicPartitionOffset.Offset, out result);

                cancellationToken.ThrowIfCancellationRequested();

                if (Assignment.Contains(topicPartitionOffset.TopicPartition))
                {
                    if (pulled)
                    {
                        _currentOffsets[result!.Topic][result.Partition] = result.Offset + 1;
                        return true;
                    }

                    if (EnablePartitionEof && GetEofMessageIfNeeded(topicPartitionOffset, out result))
                        return true;
                }
            }

            result = null;
            return false;
        }

        private bool IsPaused(string topic, Partition partition)
        {
            lock (_pausedPartitions)
            {
                return _pausedPartitions.Contains(new TopicPartition(topic, partition));
            }
        }

        private TopicPartitionOffset GetStartingOffset(TopicPartitionOffset topicPartitionOffset)
        {
            if (!topicPartitionOffset.Offset.IsSpecial)
                return topicPartitionOffset;

            var topic = _topics.Get(topicPartitionOffset.Topic, _config);

            var offset = topicPartitionOffset.Offset;

            if (offset == Offset.Stored || offset == Offset.Unset)
                offset = topic.GetCommittedOffset(topicPartitionOffset.Partition, GroupId);

            if (offset == Offset.End)
                offset = topic.GetLastOffset(topicPartitionOffset.Partition);
            else if (offset.IsSpecial)
                offset = topic.GetFirstOffset(topicPartitionOffset.Partition);

            // If the partition is empty the first offset would be Offset.Unset
            if (offset.IsSpecial)
                offset = new Offset(0);

            return new TopicPartitionOffset(topicPartitionOffset.TopicPartition, offset);
        }

        [SuppressMessage("", "CA1031", Justification = "Ensures retry in next iteration")]
        private async Task AutoCommitAsync()
        {
            while (!Disposed)
            {
                try
                {
                    Commit();
                }
                catch (Exception)
                {
                    // Ignore
                }

                await Task.Delay(_autoCommitIntervalMs).ConfigureAwait(false);
            }
        }

        private bool GetEofMessageIfNeeded(
            TopicPartitionOffset topicPartitionOffset,
            out ConsumeResult<byte[]?, byte[]?>? result)
        {
            if (!_lastPartitionEof.ContainsKey(topicPartitionOffset.Topic))
            {
                _lastPartitionEof[topicPartitionOffset.Topic] =
                    new ConcurrentDictionary<Partition, Offset>();
            }

            if (!_lastPartitionEof[topicPartitionOffset.Topic]
                .ContainsKey(topicPartitionOffset.Partition))
            {
                _lastPartitionEof[topicPartitionOffset.Topic][topicPartitionOffset.Partition] = -1;
            }

            var lastEofOffset =
                _lastPartitionEof[topicPartitionOffset.Topic][topicPartitionOffset.Partition];

            if (lastEofOffset < topicPartitionOffset.Offset)
            {
                _lastPartitionEof[topicPartitionOffset.Topic][topicPartitionOffset.Partition] =
                    topicPartitionOffset.Offset;

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

        private void EnsureNotDisposed()
        {
            if (Disposed)
                throw new ObjectDisposedException(GetType().FullName);
        }
    }
}
