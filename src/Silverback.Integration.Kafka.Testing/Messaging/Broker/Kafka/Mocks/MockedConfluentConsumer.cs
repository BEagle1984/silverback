// Copyright (c) 2020 Sergio Aquilini
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

namespace Silverback.Messaging.Broker.Kafka.Mocks
{
    internal sealed class MockedConfluentConsumer : IMockedConfluentConsumer
    {
        private readonly IInMemoryTopicCollection _topics;

        private readonly IMockedConsumerGroup _consumerGroup;

        private readonly IMockedKafkaOptions _options;

        private readonly Dictionary<TopicPartition, TopicPartitionOffset> _currentOffsets = new();

        private readonly Dictionary<TopicPartition, TopicPartitionOffset> _storedOffsets = new();

        private readonly Dictionary<TopicPartition, Offset> _lastEofOffsets = new();

        private readonly int _autoCommitIntervalMs;

        private readonly List<TopicPartition> _pausedPartitions = new();

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

            _consumerGroup = !string.IsNullOrEmpty(Config.GroupId)
                ? consumerGroups.Get(Config)
                : consumerGroups.Get(Guid.NewGuid().ToString(), config.BootstrapServers);

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

        public bool PartitionsAssigned { get; private set; }

        public List<TopicPartition> Assignment { get; } = new();

        public List<string> Subscription { get; } = new();

        public IConsumerGroupMetadata ConsumerGroupMetadata => throw new NotSupportedException();

        public bool Disposed { get; private set; }

        internal Action<IConsumer<byte[]?, byte[]?>, string>? StatisticsHandler { get; set; }

        internal Action<IConsumer<byte[]?, byte[]?>, Error>? ErrorHandler { get; set; }

        internal Func<IConsumer<byte[]?, byte[]?>, List<TopicPartition>, IEnumerable<TopicPartitionOffset>>?
            PartitionsAssignedHandler
        { get; set; }

        internal Func<IConsumer<byte[]?, byte[]?>, List<TopicPartitionOffset>,
            IEnumerable<TopicPartitionOffset>>? PartitionsRevokedHandler
        { get; set; }

        internal Action<IConsumer<byte[]?, byte[]?>, CommittedOffsets>? OffsetsCommittedHandler { get; set; }

        public int AddBrokers(string brokers) => throw new NotSupportedException();

        public ConsumeResult<byte[]?, byte[]?> Consume(int millisecondsTimeout) =>
            throw new NotSupportedException();

        public ConsumeResult<byte[]?, byte[]?> Consume(CancellationToken cancellationToken = default)
        {
            while (true)
            {
                EnsureNotDisposed();

                if (TryConsume(cancellationToken, out ConsumeResult<byte[]?, byte[]?>? result))
                    return result!;

                Thread.Sleep(10);
            }
        }

        public ConsumeResult<byte[]?, byte[]?> Consume(TimeSpan timeout) => throw new NotSupportedException();

        public void Subscribe(IEnumerable<string> topics)
        {
            EnsureNotDisposed();
            CheckGroupIdNotEmpty();

            var topicsList = Check.NotNull(topics, nameof(topics)).AsReadOnlyList();
            Check.NotEmpty(topicsList, nameof(topics));
            Check.HasNoEmpties(topicsList, nameof(topics));

            Subscription.Clear();
            Subscription.AddRange(topicsList);
            _consumerGroup.Subscribe(this, topicsList);
        }

        public void Subscribe(string topic) => Subscribe(new[] { topic });

        public void Unsubscribe()
        {
            EnsureNotDisposed();
            CheckGroupIdNotEmpty();

            _consumerGroup.Unsubscribe(this);
            Subscription.Clear();
        }

        public void Assign(TopicPartition partition) =>
            Assign(new[] { new TopicPartitionOffset(partition, Offset.Unset) });

        public void Assign(TopicPartitionOffset partition) => Assign(new[] { partition });

        public void Assign(IEnumerable<TopicPartitionOffset> partitions)
        {
            Assignment.Clear();

            var partitionsList = Check.NotNull(partitions, nameof(partitions)).AsReadOnlyList();

            foreach (var topicPartitionOffset in partitionsList)
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

            EnsureNotDisposed();

            StoreOffset(result.TopicPartitionOffset);
        }

        public void StoreOffset(TopicPartitionOffset offset)
        {
            EnsureNotDisposed();

            Check.NotNull(offset, nameof(offset));

            lock (_storedOffsets)
            {
                _storedOffsets[offset.TopicPartition] = offset;
            }
        }

        public List<TopicPartitionOffset> Commit()
        {
            EnsureNotDisposed();
            CheckGroupIdNotEmpty();

            lock (_storedOffsets)
            {
                if (_storedOffsets.Count == 0)
                    return new List<TopicPartitionOffset>();

                List<TopicPartitionOffset> committedOffsets = _storedOffsets.Values.ToList();
                _consumerGroup.Commit(committedOffsets);
                _storedOffsets.Clear();

                List<TopicPartitionOffsetError> topicPartitionOffsetErrors = committedOffsets
                    .Select(
                        topicPartitionOffset =>
                            new TopicPartitionOffsetError(topicPartitionOffset, null))
                    .ToList();
                OffsetsCommittedHandler?.Invoke(this, new CommittedOffsets(topicPartitionOffsetErrors, null));

                return committedOffsets;
            }
        }

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

        public TopicPartitionOffset GetStoredOffset(TopicPartition topicPartition)
        {
            lock (_storedOffsets)
            {
                return _storedOffsets.TryGetValue(topicPartition, out var topicPartitionOffset)
                    ? topicPartitionOffset
                    : new TopicPartitionOffset(topicPartition, Offset.Unset);
            }
        }

        public void Close()
        {
            _consumerGroup.Remove(this);
        }

        public void Dispose()
        {
            Close();
            Disposed = true;
        }

        internal void OnRebalancing()
        {
            PartitionsAssigned = false;
        }

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

            foreach (var topicPartition in topicPartitions)
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

        private bool TryConsume(
            CancellationToken cancellationToken,
            out ConsumeResult<byte[]?, byte[]?>? result)
        {
            cancellationToken.ThrowIfCancellationRequested();

            EnsurePartitionsAssigned(cancellationToken);

            // Process the assigned partitions starting from the one that consumed less messages
            var topicPartitionsOffsets =
                _currentOffsets.Values
                    .Where(
                        topicPartitionOffset => !IsPaused(
                            topicPartitionOffset.Topic,
                            topicPartitionOffset.Partition))
                    .OrderBy(topicPartitionOffset => (int)topicPartitionOffset.Offset);

            cancellationToken.ThrowIfCancellationRequested();

            foreach (var topicPartitionOffset in topicPartitionsOffsets)
            {
                var inMemoryPartition =
                    _topics.Get(topicPartitionOffset.Topic, Config)
                        .Partitions[topicPartitionOffset.Partition];

                bool pulled = inMemoryPartition.TryPull(topicPartitionOffset.Offset, out result);

                cancellationToken.ThrowIfCancellationRequested();

                if (!Assignment.Contains(topicPartitionOffset.TopicPartition))
                    continue;

                if (pulled)
                {
                    _currentOffsets[result!.TopicPartition] =
                        new TopicPartitionOffset(result.TopicPartition, result.Offset + 1);

                    return true;
                }

                if (Config.EnablePartitionEof == true &&
                    GetEofMessageIfNeeded(topicPartitionOffset, out result))
                {
                    return true;
                }
            }

            result = null;
            return false;
        }

        private void EnsurePartitionsAssigned(CancellationToken cancellationToken)
        {
            if (PartitionsAssigned)
                return;

            while (_consumerGroup.IsRebalancing)
            {
                Task.Delay(10, cancellationToken).Wait(cancellationToken);
            }

            if (_options.PartitionsAssignmentDelay > TimeSpan.Zero)
                Task.Delay(_options.PartitionsAssignmentDelay, cancellationToken).Wait(cancellationToken);

            IReadOnlyCollection<TopicPartition> assignedPartitions = _consumerGroup
                .GetAssignment(this)
                .Where(topicPartition => !Assignment.Contains(topicPartition))
                .ToList();

            var partitionOffsets =
                PartitionsAssignedHandler?.Invoke(
                        this,
                        assignedPartitions.AsList())
                    ?.ToList()
                ?? assignedPartitions
                    .Select(topicPartition => new TopicPartitionOffset(topicPartition, Offset.Unset))
                    .ToList();

            foreach (var partitionOffset in partitionOffsets)
            {
                Assignment.Add(partitionOffset.TopicPartition);
                Seek(GetStartingOffset(partitionOffset));
            }

            PartitionsAssigned = true;
        }

        private TopicPartitionOffset GetStartingOffset(TopicPartitionOffset topicPartitionOffset)
        {
            if (!topicPartitionOffset.Offset.IsSpecial)
                return topicPartitionOffset;

            var offset = topicPartitionOffset.Offset;

            if (offset == Offset.Stored || offset == Offset.Unset)
                offset = _consumerGroup.GetCommittedOffset(topicPartitionOffset.TopicPartition)?.Offset ?? Offset.Unset;

            var topic = _topics.Get(topicPartitionOffset.Topic, Config);

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
            if (!_lastEofOffsets.ContainsKey(topicPartitionOffset.TopicPartition))
            {
                _lastEofOffsets[topicPartitionOffset.TopicPartition] = -1;
            }

            var lastEofOffset = _lastEofOffsets[topicPartitionOffset.TopicPartition];

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

        private void EnsureNotDisposed()
        {
            if (Disposed)
                throw new ObjectDisposedException(GetType().FullName);
        }
    }
}
