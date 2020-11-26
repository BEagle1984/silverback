// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Topics
{
    /// <inheritdoc cref="IInMemoryTopic" />
    public class InMemoryTopic : IInMemoryTopic
    {
        private readonly List<InMemoryPartition> _partitions;

        private readonly object _consumersLock;

        private readonly List<MockedKafkaConsumer> _consumers = new();

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<Partition, Offset>> _committedOffsets =
            new();

        private readonly List<string> _groupsPendingRebalance = new();

        /// <summary>
        ///     Initializes a new instance of the <see cref="InMemoryTopic" /> class.
        /// </summary>
        /// <param name="name">
        ///     The name of the topic.
        /// </param>
        /// <param name="partitions">
        ///     The number of partitions to create.
        /// </param>
        /// <param name="consumersLock">
        ///     The object to be locked when an operation is performed on the consumers (e.g. a rebalance).
        /// </param>
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

        /// <inheritdoc cref="IInMemoryTopic.Name" />
        public string Name { get; }

        /// <inheritdoc cref="IInMemoryTopic.PartitionsCount" />
        public int PartitionsCount => _partitions.Count;

        /// <inheritdoc cref="IInMemoryTopic.TotalMessagesCount" />
        public int TotalMessagesCount => _partitions.Sum(partition => partition.TotalMessagesCount);

        /// <inheritdoc cref="IInMemoryTopic.Push" />
        public Offset Push(int partition, Message<byte[]?, byte[]?> message) =>
            _partitions[partition].Add(message);

        /// <inheritdoc cref="IInMemoryTopic.TryPull" />
        public bool TryPull(
            string groupId,
            IReadOnlyCollection<TopicPartitionOffset> partitionOffsets,
            out ConsumeResult<byte[]?, byte[]?>? result)
        {
            try
            {
                if (!_committedOffsets.ContainsKey(groupId))
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

        /// <inheritdoc cref="IInMemoryTopic.Subscribe" />
        public void Subscribe(MockedKafkaConsumer consumer)
        {
            Check.NotNull(consumer, nameof(consumer));

            lock (_consumersLock)
            {
                _consumers.Add(consumer);

                if (!_committedOffsets.ContainsKey(consumer.GroupId))
                {
                    _committedOffsets[consumer.GroupId] = new ConcurrentDictionary<Partition, Offset>(
                        _partitions.Select(
                            partition =>
                                new KeyValuePair<Partition, Offset>(partition.Partition.Value, Offset.Unset)));
                }

                if (!_groupsPendingRebalance.Contains(consumer.GroupId))
                {
                    _groupsPendingRebalance.Add(consumer.GroupId);

                    // Rebalance asynchronously to mimic the real Kafka
                    Task.Run(() => Rebalance(consumer.GroupId));
                }
            }
        }

        /// <inheritdoc cref="IInMemoryTopic.Unsubscribe" />
        public void Unsubscribe(MockedKafkaConsumer consumer)
        {
            Check.NotNull(consumer, nameof(consumer));

            lock (_consumersLock)
            {
                _consumers.Remove(consumer);

                Rebalance(consumer.GroupId);
            }
        }

        /// <inheritdoc cref="IInMemoryTopic.Commit" />
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

        /// <inheritdoc cref="IInMemoryTopic.GetCommittedOffset" />
        public Offset GetCommittedOffset(Partition partition, string groupId) =>
            _committedOffsets.ContainsKey(groupId)
                ? _committedOffsets[groupId]
                    .FirstOrDefault(partitionPair => partitionPair.Key == partition).Value
                : Offset.Unset;

        /// <inheritdoc cref="IInMemoryTopic.GetCommittedOffsets" />
        public IReadOnlyCollection<TopicPartitionOffset> GetCommittedOffsets(string groupId) =>
            _committedOffsets.ContainsKey(groupId)
                ? _committedOffsets[groupId]
                    .Select(partitionPair => new TopicPartitionOffset(Name, partitionPair.Key, partitionPair.Value))
                    .ToArray()
                : Array.Empty<TopicPartitionOffset>();

        /// <inheritdoc cref="IInMemoryTopic.GetCommittedOffsetsCount" />
        public long GetCommittedOffsetsCount(string groupId) =>
            GetCommittedOffsets(groupId).Sum(topicPartitionOffset => Math.Max(0, topicPartitionOffset.Offset));

        /// <inheritdoc cref="IInMemoryTopic.GetFirstOffset" />
        public Offset GetFirstOffset(Partition partition)
            => _partitions[partition].FirstOffset;

        /// <inheritdoc cref="IInMemoryTopic.GetLastOffset" />
        public Offset GetLastOffset(Partition partition)
            => _partitions[partition].LastOffset;

        /// <inheritdoc cref="IInMemoryTopic.WaitUntilAllMessagesAreConsumedAsync" />
        public async Task WaitUntilAllMessagesAreConsumedAsync(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // ReSharper disable once InconsistentlySynchronizedField
                if (_consumers.All(HasFinishedConsuming))
                    return;

                await Task.Delay(50, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc cref="IInMemoryTopic.Rebalance" />
        public void Rebalance()
        {
            lock (_consumersLock)
            {
                _consumers
                    .Select(consumer => consumer.GroupId)
                    .Distinct()
                    .ForEach(Rebalance);
            }
        }

        private void Rebalance(string groupId)
        {
            lock (_consumersLock)
            {
                _groupsPendingRebalance.Remove(groupId);

                _consumers
                    .Where(consumer => consumer.Disposed)
                    .ToList()
                    .ForEach(consumer => _consumers.Remove(consumer));

                var groupConsumers = _consumers.Where(consumer => consumer.GroupId == groupId).ToList();

                groupConsumers.ForEach(consumer => consumer.OnRebalancing());

                var assignments = new List<Partition>[groupConsumers.Count];

                for (int i = 0; i < groupConsumers.Count; i++)
                {
                    assignments[i] = new List<Partition>();
                }

                for (int partitionIndex = 0; partitionIndex < _partitions.Count; partitionIndex++)
                {
                    assignments[partitionIndex % groupConsumers.Count].Add(new Partition(partitionIndex));
                }

                for (int i = 0; i < groupConsumers.Count; i++)
                {
                    groupConsumers[i].OnPartitionsAssigned(Name, assignments[i]);
                }
            }
        }

        private bool HasFinishedConsuming(MockedKafkaConsumer consumer)
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
