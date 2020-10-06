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
    /// <inheritdoc cref="IInMemoryTopic" />
    public class InMemoryTopic : IInMemoryTopic
    {
        private readonly List<InMemoryPartition> _partitions;

        private readonly List<MockedKafkaConsumer> _consumers = new List<MockedKafkaConsumer>();

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<Partition, Offset>> _committedOffsets =
            new ConcurrentDictionary<string, ConcurrentDictionary<Partition, Offset>>();

        /// <summary>
        ///     Initializes a new instance of the <see cref="InMemoryTopic" /> class.
        /// </summary>
        /// <param name="name">
        ///     The name of the topic.
        /// </param>
        /// <param name="partitions">
        ///     The number of partitions to create. The default is 5.
        /// </param>
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

        /// <inheritdoc cref="IInMemoryTopic.Name" />
        public string Name { get; }

        /// <inheritdoc cref="IInMemoryTopic.Push" />
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public Offset Push(int partition, Message<byte[]?, byte[]?> message) =>
            _partitions[partition].Add(message);

        /// <inheritdoc cref="IInMemoryTopic.Pull" />
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
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

        /// <inheritdoc cref="IInMemoryTopic.Subscribe" />
        public void Subscribe(MockedKafkaConsumer consumer)
        {
            Check.NotNull(consumer, nameof(consumer));

            lock (_consumers)
            {
                _consumers.Add(consumer);

                if (!_committedOffsets.ContainsKey(consumer.GroupId))
                {
                    _committedOffsets[consumer.GroupId] = new ConcurrentDictionary<Partition, Offset>(
                        _partitions.Select(
                            partition =>
                                new KeyValuePair<Partition, Offset>(partition.Partition.Value, Offset.Unset)));
                }
            }

            RebalancePartitions(consumer.GroupId);
        }

        /// <inheritdoc cref="IInMemoryTopic.Unsubscribe" />
        public void Unsubscribe(MockedKafkaConsumer consumer)
        {
            Check.NotNull(consumer, nameof(consumer));

            lock (_consumers)
            {
                _consumers.Remove(consumer);
            }

            RebalancePartitions(consumer.GroupId);
        }

        /// <inheritdoc cref="IInMemoryTopic.Commit" />
        public void Commit(string groupId, IEnumerable<TopicPartitionOffset> partitionOffsets)
        {
            Check.NotNull(partitionOffsets, nameof(partitionOffsets));

            foreach (var partitionOffset in partitionOffsets)
            {
                _committedOffsets[groupId][partitionOffset.Partition] = partitionOffset.Offset;
            }
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

        /// <inheritdoc cref="IInMemoryTopic.GetLatestOffset" />
        public Offset GetLatestOffset(Partition partition)
            => _partitions[partition].LatestOffset;

        /// <inheritdoc cref="IInMemoryTopic.WaitUntilAllMessagesAreConsumed" />
        [SuppressMessage("", "CA2000", Justification = Justifications.NewUsingSyntaxFalsePositive)]
        public async Task WaitUntilAllMessagesAreConsumed(TimeSpan? timeout = null)
        {
            using var cancellationTokenSource = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(60));

            try
            {
                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    // ReSharper disable once InconsistentlySynchronizedField
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

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        private bool TryPull(
            string groupId,
            IReadOnlyCollection<TopicPartitionOffset> partitionOffsets,
            out ConsumeResult<byte[]?, byte[]?>? result)
        {
            if (!_committedOffsets.ContainsKey(groupId))
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

            var partitionsOffsets = _committedOffsets[consumer.GroupId];

            return consumer.Assignment.All(
                topicPartition =>
                {
                    var lastOffset = _partitions[topicPartition.Partition].LatestOffset;
                    return lastOffset < 0 || partitionsOffsets[topicPartition.Partition] > lastOffset;
                });
        }
    }
}
