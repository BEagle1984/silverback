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
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka.Mocks
{
    internal class InMemoryTopic : IInMemoryTopic
    {
        private readonly List<InMemoryPartition> _partitions;

        private readonly object _consumersLock;

        private readonly List<MockedConfluentConsumer> _subscribedConsumers = new();

        private readonly List<MockedConfluentConsumer> _assignedConsumers = new();

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<Partition, Offset>>
            _committedOffsets = new();

        private readonly List<string> _groupsPendingRebalance = new();

        private readonly Dictionary<string, Dictionary<IMockedConfluentConsumer, List<Partition>>>
            _assignmentsDictionary = new();

        public InMemoryTopic(string name, int partitions, object consumersLock)
        {
            Name = Check.NotEmpty(name, nameof(name));

            if (partitions < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(partitions),
                    partitions,
                    "The number of partition must be a positive number greater or equal to 1.");
            }

            _partitions = new List<InMemoryPartition>(
                Enumerable.Range(0, partitions)
                    .Select(i => new InMemoryPartition(i, this)));

            _consumersLock = Check.NotNull(consumersLock, nameof(consumersLock));
        }

        public string Name { get; }

        public IReadOnlyList<IInMemoryPartition> Partitions => _partitions;

        public int TotalMessagesCount => _partitions.Sum(partition => partition.TotalMessagesCount);

        public Offset Push(int partition, Message<byte[]?, byte[]?> message) =>
            _partitions[partition].Add(message);

        public bool TryPull(
            IMockedConfluentConsumer consumer,
            IReadOnlyCollection<TopicPartitionOffset> partitionOffsets,
            out ConsumeResult<byte[]?, byte[]?>? result)
        {
            try
            {
                if (!_committedOffsets.ContainsKey(consumer.GroupId))
                {
                    result = null;
                    return false;
                }

                foreach (var partitionOffset in partitionOffsets.OrderBy(
                    partitionOffset => partitionOffset.Offset.Value))
                {
                    if (_partitions[partitionOffset.Partition].TryPull(partitionOffset.Offset, out result))
                        return true;
                }

                result = null;
                return false;
            }
            catch (Exception exception)
            {
                throw new KafkaException(new Error(ErrorCode.Unknown), exception);
            }
        }

        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "Sync writes only")]
        public void EnsurePartitionsAssigned(
            IMockedConfluentConsumer consumer,
            CancellationToken cancellationToken)
        {
            if (consumer.PartitionsAssigned)
                return;

            while (!_assignmentsDictionary.ContainsKey(consumer.GroupId) ||
                   !_assignmentsDictionary[consumer.GroupId].ContainsKey(consumer))
            {
                Task.Delay(10, cancellationToken).Wait(cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();

            lock (_consumersLock)
            {
                ((MockedConfluentConsumer)consumer).OnPartitionsAssigned(
                    Name,
                    _assignmentsDictionary[consumer.GroupId][consumer]);
            }
        }

        public void Subscribe(IMockedConfluentConsumer consumer)
        {
            Check.NotNull(consumer, nameof(consumer));

            lock (_consumersLock)
            {
                _subscribedConsumers.Add((MockedConfluentConsumer)consumer);

                InitCommittedOffsetsDictionary(consumer);
                ScheduleRebalance(consumer.GroupId);
            }
        }

        public void Unsubscribe(IMockedConfluentConsumer consumer)
        {
            Check.NotNull(consumer, nameof(consumer));

            lock (_consumersLock)
            {
                _subscribedConsumers.Remove((MockedConfluentConsumer)consumer);

                Rebalance(consumer.GroupId);
            }
        }

        public void Assign(IMockedConfluentConsumer consumer, Partition partition)
        {
            Check.NotNull(consumer, nameof(consumer));

            lock (_consumersLock)
            {
                if (!_assignedConsumers.Contains(consumer))
                    _assignedConsumers.Add((MockedConfluentConsumer)consumer);

                InitCommittedOffsetsDictionary(consumer);
                ScheduleRebalance(consumer.GroupId);
            }
        }

        public IReadOnlyCollection<TopicPartitionOffset> Commit(
            string groupId,
            IEnumerable<TopicPartitionOffset> partitionOffsets)
        {
            Check.NotNull(partitionOffsets, nameof(partitionOffsets));

            var actualCommittedOffsets = new List<TopicPartitionOffset>();

            foreach (var partitionOffset in partitionOffsets)
            {
                if (_committedOffsets[groupId][partitionOffset.Partition] >= partitionOffset.Offset)
                    continue;

                _committedOffsets[groupId][partitionOffset.Partition] = partitionOffset.Offset;
                actualCommittedOffsets.Add(partitionOffset);
            }

            return actualCommittedOffsets;
        }

        public Offset GetCommittedOffset(Partition partition, string groupId) =>
            _committedOffsets.ContainsKey(groupId)
                ? _committedOffsets[groupId]
                    .FirstOrDefault(partitionPair => partitionPair.Key == partition).Value
                : Offset.Unset;

        public IReadOnlyCollection<TopicPartitionOffset> GetCommittedOffsets(string groupId) =>
            _committedOffsets.ContainsKey(groupId)
                ? _committedOffsets[groupId]
                    .Select(
                        partitionPair => new TopicPartitionOffset(
                            Name,
                            partitionPair.Key,
                            partitionPair.Value))
                    .ToArray()
                : Array.Empty<TopicPartitionOffset>();

        public long GetCommittedOffsetsCount(string groupId) =>
            GetCommittedOffsets(groupId)
                .Sum(topicPartitionOffset => Math.Max(0, topicPartitionOffset.Offset));

        public Offset GetFirstOffset(Partition partition)
            => _partitions[partition].FirstOffset;

        public Offset GetLastOffset(Partition partition)
            => _partitions[partition].LastOffset;

        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "Sync writes only")]
        public async Task WaitUntilAllMessagesAreConsumedAsync(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_subscribedConsumers.Union(_assignedConsumers).All(HasFinishedConsuming))
                    return;

                await Task.Delay(10, cancellationToken).ConfigureAwait(false);
            }
        }

        public void Rebalance()
        {
            lock (_consumersLock)
            {
                _subscribedConsumers
                    .Select(consumer => consumer.GroupId)
                    .Distinct()
                    .ForEach(Rebalance);
            }
        }

        private void InitCommittedOffsetsDictionary(IMockedConfluentConsumer consumer)
        {
            if (!_committedOffsets.ContainsKey(consumer.GroupId))
            {
                _committedOffsets[consumer.GroupId] = new ConcurrentDictionary<Partition, Offset>(
                    _partitions.Select(
                        partition =>
                            new KeyValuePair<Partition, Offset>(
                                partition.Partition.Value,
                                Offset.Unset)));
            }
        }

        [SuppressMessage("", "VSTHRD110", Justification = Justifications.FireAndForget)]
        private void ScheduleRebalance(string groupId)
        {
            if (!_groupsPendingRebalance.Contains(groupId))
            {
                _groupsPendingRebalance.Add(groupId);

                // Rebalance asynchronously to mimic the real Kafka
                Task.Run(() => Rebalance(groupId));
            }
        }

        private void Rebalance(string groupId)
        {
            lock (_consumersLock)
            {
                _groupsPendingRebalance.Remove(groupId);

                _subscribedConsumers
                    .Where(consumer => consumer.Disposed)
                    .ToList()
                    .ForEach(consumer => _subscribedConsumers.Remove(consumer));
                _assignedConsumers
                    .Where(consumer => consumer.Disposed)
                    .ToList()
                    .ForEach(consumer => _assignedConsumers.Remove(consumer));

                var groupConsumers = _subscribedConsumers.Where(consumer => consumer.GroupId == groupId)
                    .ToList();
                var groupAssignedConsumers =
                    _assignedConsumers.Where(consumer => consumer.GroupId == groupId).ToList();

                groupConsumers.ForEach(consumer => consumer.OnRebalancing());

                var assignments = new List<Partition>[groupConsumers.Count];

                for (int i = 0; i < groupConsumers.Count; i++)
                {
                    assignments[i] = new List<Partition>();
                }

                for (int partitionIndex = 0; partitionIndex < _partitions.Count; partitionIndex++)
                {
                    // Skip manually assigned partitions
                    if (groupAssignedConsumers.Any(
                        consumer => consumer.Assignment.Contains(new TopicPartition(Name, partitionIndex))))
                        continue;

                    assignments[partitionIndex % groupConsumers.Count].Add(new Partition(partitionIndex));
                }

                _assignmentsDictionary[groupId] = new Dictionary<IMockedConfluentConsumer, List<Partition>>();
                for (int i = 0; i < groupConsumers.Count; i++)
                {
                    _assignmentsDictionary[groupId][groupConsumers[i]] = assignments[i];
                }
            }
        }

        private bool HasFinishedConsuming(IMockedConfluentConsumer consumer)
        {
            if (consumer.Disposed)
                return true;

            if (!consumer.PartitionsAssigned)
                return false;

            var partitionsOffsets = _committedOffsets[consumer.GroupId];

            return consumer.Assignment.All(
                topicPartition =>
                {
                    var lastOffset = _partitions[topicPartition.Partition].LastOffset;
                    return lastOffset < 0 || partitionsOffsets[topicPartition.Partition] > lastOffset;
                });
        }
    }
}
