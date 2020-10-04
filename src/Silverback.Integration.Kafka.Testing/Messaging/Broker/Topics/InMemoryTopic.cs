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

namespace Silverback.Messaging.Broker.Topics
{
    public class InMemoryTopic : IInMemoryTopic
    {
        private readonly List<InMemoryPartition> _partitions;

        private readonly List<MockedKafkaConsumer> _consumers = new List<MockedKafkaConsumer>();

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<Partition, Offset>> _offsets =
            new ConcurrentDictionary<string, ConcurrentDictionary<Partition, Offset>>();

        public InMemoryTopic(string name, int partitions = 5)
        {
            Name = Check.NotEmpty(name, nameof(name));

            if (partitions <= 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(partitions),
                    partitions,
                    "The number of partition must be a positive number greater or equal to 1.");
            }

            _partitions = new List<InMemoryPartition>(
                Enumerable.Range(0, partitions)
                    .Select(i => new InMemoryPartition(i, this)));
        }

        public string Name { get; }

        public Offset Push(int partition, Message<byte[], byte[]> message) =>
            _partitions[partition].Add(message);

        public ConsumeResult<byte[]?, byte[]?> Pull(
            string groupId,
            IReadOnlyCollection<TopicPartitionOffset> partitionOffsets,
            CancellationToken cancellationToken)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (TryPull(groupId, partitionOffsets, out var result))
                    return result!;

                Task.Delay(TimeSpan.FromMilliseconds(10), cancellationToken).Wait(cancellationToken);
            }
        }

        public void Subscribe(MockedKafkaConsumer consumer)
        {
            lock (_consumers)
            {
                _consumers.Add(consumer);

                if (!_offsets.ContainsKey(consumer.GroupId))
                {
                    _offsets[consumer.GroupId] = new ConcurrentDictionary<Partition, Offset>(
                        _partitions.Select(
                            partition =>
                                new KeyValuePair<Partition, Offset>(partition.Partition.Value, Offset.Unset)));
                }
            }

            RebalancePartitions(consumer.GroupId);
        }

        public void Unsubscribe(MockedKafkaConsumer consumer)
        {
            Check.NotNull(consumer, nameof(consumer));

            lock (_consumers)
            {
                _consumers.Remove(consumer);
            }

            RebalancePartitions(consumer.GroupId);
        }

        public void Commit(string groupId, IEnumerable<TopicPartitionOffset> partitionOffsets)
        {
            foreach (var partitionOffset in partitionOffsets)
            {
                _offsets[groupId][partitionOffset.Partition] = partitionOffset.Offset;
            }
        }

        public Offset GetCommittedOffset(Partition partition, string groupId) =>
            _offsets.ContainsKey(groupId)
                ? _offsets[groupId]
                    .FirstOrDefault(partitionPair => partitionPair.Key == partition).Value
                : Offset.Unset;

        public IReadOnlyCollection<TopicPartitionOffset> GetCommittedOffsets(string groupId) =>
            _offsets.ContainsKey(groupId)
                ? _offsets[groupId]
                    .Select(partitionPair => new TopicPartitionOffset(Name, partitionPair.Key, partitionPair.Value))
                    .ToArray()
                : Array.Empty<TopicPartitionOffset>();

        public long GetCommittedOffsetsCount(string groupId) =>
            GetCommittedOffsets("consumer1").Sum(topicPartitionOffset => Math.Max(0, topicPartitionOffset.Offset));

        public Offset GetLastOffset(Partition partition)
            => _partitions[partition].LastOffset;

        /// <summary>
        ///     Returns a <see cref="Task" /> that completes when all messages routed to the consumers have been
        ///     processed.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> that completes when all messages have been processed.
        /// </returns>
        [SuppressMessage("", "CA2000", Justification = Justifications.NewUsingSyntaxFalsePositive)]
        public async Task WaitUntilAllMessagesAreConsumed(TimeSpan? timeout = null)
        {
            using var cancellationTokenSource = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(60));

            try
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    if (_consumers.All(HasFinishedConsuming))
                        return;

                    await Task.Delay(100, cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
        }

        private bool TryPull(
            string groupId,
            IReadOnlyCollection<TopicPartitionOffset> partitionOffsets,
            out ConsumeResult<byte[], byte[]>? result)
        {
            if (!_offsets.ContainsKey(groupId))
            {
                result = null;
                return false;
            }

            foreach (var partitionOffset in partitionOffsets.OrderBy(partitionOffset => partitionOffset.Offset.Value))
            {
                if (_partitions[partitionOffset.Partition].TryPull(partitionOffset.Offset, out result))
                    return true;
            }

            result = null;
            return false;
        }

        private void RebalancePartitions(string groupId)
        {
            lock (_consumers)
            {
                var groupConsumers = _consumers.Where(consumer => consumer.GroupId == groupId).ToList();

                var assignments = new List<Partition>[groupConsumers.Count];

                Enumerable.Range(0, groupConsumers.Count).ForEach(i => assignments[i] = new List<Partition>());

                for (int partitionIndex = 0; partitionIndex < _partitions.Count; partitionIndex++)
                {
                    assignments[partitionIndex % groupConsumers.Count]
                        .Add(new Partition(partitionIndex));
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

            var partitionsOffsets = _offsets[consumer.GroupId];

            return consumer.Assignment.All(
                topicPartition =>
                {
                    var lastOffset = _partitions[topicPartition.Partition].LastOffset;
                    return lastOffset < 0 || partitionsOffsets[topicPartition.Partition] > lastOffset;
                });
        }
    }
}
