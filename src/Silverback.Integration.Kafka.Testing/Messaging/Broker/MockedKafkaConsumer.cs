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
using Silverback.Messaging.Broker.Topics;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     A mocked implementation of <see cref="IConsumer{TKey,TValue}" /> from Confluent.Kafka that consumes
    ///     from an <see cref="InMemoryTopic" />.
    /// </summary>
    [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
    public sealed class MockedKafkaConsumer : IConsumer<byte[]?, byte[]?>
    {
        private readonly ConsumerConfig _config;

        private readonly IInMemoryTopicCollection _topics;

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<Partition, Offset>> _currentOffsets =
            new ConcurrentDictionary<string, ConcurrentDictionary<Partition, Offset>>();

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<Partition, Offset>> _storedOffsets =
            new ConcurrentDictionary<string, ConcurrentDictionary<Partition, Offset>>();

        private readonly List<TopicPartitionOffset> _temporaryAssignment = new List<TopicPartitionOffset>();

        private readonly List<string> _topicAssignments = new List<string>();

        /// <summary>
        ///     Initializes a new instance of the <see cref="MockedKafkaConsumer" /> class.
        /// </summary>
        /// <param name="config">
        ///     The consumer configuration.
        /// </param>
        /// <param name="topics">
        ///     The collection of <see cref="InMemoryTopic" />.
        /// </param>
        public MockedKafkaConsumer(ConsumerConfig config, IInMemoryTopicCollection topics)
        {
            _config = Check.NotNull(config, nameof(config));
            _topics = Check.NotNull(topics, nameof(topics));

            if (string.IsNullOrEmpty(config.GroupId))
                throw new ArgumentException("'group.id' configuration parameter is required and was not specified.");

            Name = $"{config.ClientId ?? "mocked"}.{Guid.NewGuid():N}";
            GroupId = config.GroupId;
            MemberId = Guid.NewGuid().ToString("N");

            if (_config.EnableAutoCommit ?? true)
                Task.Run(AutoCommitAsync);
        }

        /// <inheritdoc cref="IClient.Handle" />
        public Handle Handle => throw new NotSupportedException();

        /// <inheritdoc cref="IClient.Name" />
        public string Name { get; }

        /// <summary>
        ///     Gets the consumer group id from the 'group.id' configuration parameter.
        /// </summary>
        public string GroupId { get; }

        /// <inheritdoc cref="IConsumer{TKey,TValue}.MemberId" />
        public string MemberId { get; }

        /// <inheritdoc cref="IConsumer{TKey,TValue}.Assignment" />
        public List<TopicPartition> Assignment { get; } = new List<TopicPartition>();

        /// <inheritdoc cref="IConsumer{TKey,TValue}.Subscription" />
        public List<string> Subscription { get; } = new List<string>();

        /// <inheritdoc cref="IConsumer{TKey,TValue}.ConsumerGroupMetadata" />
        public IConsumerGroupMetadata ConsumerGroupMetadata => throw new NotSupportedException();

        /// <summary>
        ///     Gets a value indicating whether the partitions have been assigned to the consumer.
        /// </summary>
        /// <remarks>
        ///     This value indicates that the rebalance process is over. It could be that no partition has actually
        ///     been assigned.
        /// </remarks>
        public bool PartitionsAssigned { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether this instance was disposed.
        /// </summary>
        public bool Disposed { get; private set; }

        internal Action<IConsumer<byte[]?, byte[]?>, string>? StatisticsHandler { get; set; }

        internal Action<IConsumer<byte[]?, byte[]?>, Error>? ErrorHandler { get; set; }

        internal Func<IConsumer<byte[]?, byte[]?>, List<TopicPartition>, IEnumerable<TopicPartitionOffset>>?
            PartitionsAssignedHandler { get; set; }

        internal Func<IConsumer<byte[]?, byte[]?>, List<TopicPartitionOffset>, IEnumerable<TopicPartitionOffset>>?
            PartitionsRevokedHandler { get; set; }

        internal Action<IConsumer<byte[]?, byte[]?>, CommittedOffsets>? OffsetsCommittedHandler { get; set; }

        /// <inheritdoc cref="IConsumer{TKey,TValue}.MemberId" />
        public int AddBrokers(string brokers) => throw new NotSupportedException();

        /// <inheritdoc cref="IConsumer{TKey,TValue}.Consume(int)" />
        public ConsumeResult<byte[]?, byte[]?> Consume(int millisecondsTimeout) => throw new NotSupportedException();

        /// <inheritdoc cref="IConsumer{TKey,TValue}.Consume(CancellationToken)" />
        public ConsumeResult<byte[]?, byte[]?> Consume(CancellationToken cancellationToken = default)
        {
            while (true)
            {
                if (TryConsume(cancellationToken, out var result))
                    return result!;

                AsyncHelper.RunSynchronously(() => Task.Delay(TimeSpan.FromMilliseconds(10), cancellationToken));
            }
        }

        /// <inheritdoc cref="IConsumer{TKey,TValue}.Consume(TimeSpan)" />
        public ConsumeResult<byte[]?, byte[]?> Consume(TimeSpan timeout) => throw new NotSupportedException();

        /// <inheritdoc cref="IConsumer{TKey,TValue}.Subscribe(IEnumerable{string})" />
        public void Subscribe(IEnumerable<string> topics)
        {
            var topicsList = Check.NotNull(topics, nameof(topics)).AsReadOnlyList();
            Check.HasNoNullsOrEmpties(topicsList, nameof(topics));

            lock (Subscription)
            {
                Subscription.Clear();
                Subscription.AddRange(topicsList);
                Subscription.ForEach(topic => _topics[topic].Subscribe(this));
            }
        }

        /// <inheritdoc cref="IConsumer{TKey,TValue}.Subscribe(string)" />
        public void Subscribe(string topic) => Subscribe(new[] { topic });

        /// <inheritdoc cref="IConsumer{TKey,TValue}.Unsubscribe" />
        public void Unsubscribe()
        {
            lock (Subscription)
            {
                foreach (var topic in Subscription)
                {
                    _topics[topic].Unsubscribe(this);
                }

                Subscription.Clear();
            }
        }

        /// <inheritdoc cref="IConsumer{TKey,TValue}.Assign(TopicPartition)" />
        public void Assign(TopicPartition partition) => throw new NotSupportedException();

        /// <inheritdoc cref="IConsumer{TKey,TValue}.Assign(TopicPartitionOffset)" />
        public void Assign(TopicPartitionOffset partition) => throw new NotSupportedException();

        /// <inheritdoc cref="IConsumer{TKey,TValue}.Assign(IEnumerable{TopicPartitionOffset})" />
        public void Assign(IEnumerable<TopicPartitionOffset> partitions) => throw new NotSupportedException();

        /// <inheritdoc cref="IConsumer{TKey,TValue}.Assign(IEnumerable{TopicPartition})" />
        public void Assign(IEnumerable<TopicPartition> partitions) => throw new NotSupportedException();

        /// <inheritdoc cref="IConsumer{TKey,TValue}.Unassign" />
        public void Unassign() => throw new NotSupportedException();

        /// <inheritdoc cref="IConsumer{TKey,TValue}.StoreOffset(ConsumeResult{TKey,TValue})" />
        public void StoreOffset(ConsumeResult<byte[]?, byte[]?> result)
        {
            if (result == null)
                return;

            StoreOffset(result.TopicPartitionOffset);
        }

        /// <inheritdoc cref="IConsumer{TKey,TValue}.StoreOffset(TopicPartitionOffset)" />
        public void StoreOffset(TopicPartitionOffset offset)
        {
            if (offset == null)
                return;

            var partitionOffsetDictionary = _storedOffsets.GetOrAdd(
                offset.Topic,
                _ => new ConcurrentDictionary<Partition, Offset>());

            partitionOffsetDictionary[offset.Partition] = offset.Offset;
        }

        /// <inheritdoc cref="IConsumer{TKey,TValue}.Commit()" />
        public List<TopicPartitionOffset> Commit()
        {
            var topicPartitionOffsets = _storedOffsets.SelectMany(
                topicPair => topicPair.Value.Select(
                    partitionPair => new TopicPartitionOffset(
                        topicPair.Key,
                        partitionPair.Key,
                        partitionPair.Value))).ToList();

            var topicPartitionOffsetsByTopic = topicPartitionOffsets.GroupBy(tpo => tpo.Topic);

            var actualCommittedOffsets = new List<TopicPartitionOffset>();
            foreach (var group in topicPartitionOffsetsByTopic)
            {
                actualCommittedOffsets.AddRange(_topics[group.Key].Commit(GroupId, group));
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

        /// <inheritdoc cref="IConsumer{TKey,TValue}.Commit(IEnumerable{TopicPartitionOffset})" />
        public void Commit(IEnumerable<TopicPartitionOffset> offsets) => throw new NotSupportedException();

        /// <inheritdoc cref="IConsumer{TKey,TValue}.Commit(ConsumeResult{TKey,TValue})" />
        public void Commit(ConsumeResult<byte[]?, byte[]?> result) => throw new NotSupportedException();

        /// <inheritdoc cref="IConsumer{TKey,TValue}.Seek(TopicPartitionOffset)" />
        public void Seek(TopicPartitionOffset tpo)
        {
            Check.NotNull(tpo, nameof(tpo));

            if (!_currentOffsets.ContainsKey(tpo.Topic))
                _currentOffsets[tpo.Topic] = new ConcurrentDictionary<Partition, Offset>();

            _currentOffsets[tpo.Topic][tpo.Partition] = tpo.Offset;
        }

        /// <inheritdoc cref="IConsumer{TKey,TValue}.Pause(IEnumerable{TopicPartition})" />
        public void Pause(IEnumerable<TopicPartition> partitions) => throw new NotSupportedException();

        /// <inheritdoc cref="IConsumer{TKey,TValue}.Resume(IEnumerable{TopicPartition})" />
        public void Resume(IEnumerable<TopicPartition> partitions) => throw new NotSupportedException();

        /// <inheritdoc cref="IConsumer{TKey,TValue}.Committed(TimeSpan)" />
        public List<TopicPartitionOffset> Committed(TimeSpan timeout) => throw new NotSupportedException();

        /// <inheritdoc cref="IConsumer{TKey,TValue}.Committed(IEnumerable{TopicPartition}, TimeSpan)" />
        public List<TopicPartitionOffset> Committed(IEnumerable<TopicPartition> partitions, TimeSpan timeout) =>
            throw new NotSupportedException();

        /// <inheritdoc cref="IConsumer{TKey,TValue}.Position" />
        public Offset Position(TopicPartition partition) => throw new NotSupportedException();

        /// <inheritdoc cref="IConsumer{TKey,TValue}.OffsetsForTimes" />
        public List<TopicPartitionOffset> OffsetsForTimes(
            IEnumerable<TopicPartitionTimestamp> timestampsToSearch,
            TimeSpan timeout) =>
            throw new NotSupportedException();

        /// <inheritdoc cref="IConsumer{TKey,TValue}.GetWatermarkOffsets" />
        public WatermarkOffsets GetWatermarkOffsets(TopicPartition topicPartition) => throw new NotSupportedException();

        /// <inheritdoc cref="IConsumer{TKey,TValue}.QueryWatermarkOffsets" />
        public WatermarkOffsets QueryWatermarkOffsets(TopicPartition topicPartition, TimeSpan timeout) =>
            throw new NotSupportedException();

        /// <inheritdoc cref="IConsumer{TKey,TValue}.Close" />
        public void Close()
        {
            // Nothing to close, it's just a mock
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose() => Disposed = true;

        internal void OnRebalancing()
        {
            PartitionsAssigned = false;
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

            var partitionOffsets = _temporaryAssignment;

            var revokeHandlerResult = InvokePartitionsRevokedHandler(topicName);
            if (revokeHandlerResult != null && revokeHandlerResult.Count > 0)
            {
                partitionOffsets = partitionOffsets
                    .Union(revokeHandlerResult)
                    .GroupBy(partitionOffset => partitionOffset.TopicPartition)
                    .Select(
                        group =>
                            new TopicPartitionOffset(
                                group.Key,
                                group.Max(topicPartitionOffset => topicPartitionOffset.Offset)))
                    .ToList();
            }

            ClearPartitionsAssignment();

            var assignHandlerResult = InvokePartitionsAssignedHandler(partitionOffsets);
            if (assignHandlerResult != null)
                partitionOffsets = assignHandlerResult;

            foreach (var partitionOffset in partitionOffsets)
            {
                Assignment.Add(partitionOffset.TopicPartition);
                Seek(GetStartingOffset(partitionOffset));
            }

            PartitionsAssigned = true;
        }

        private List<TopicPartitionOffset>? InvokePartitionsAssignedHandler(
            IEnumerable<TopicPartitionOffset> partitionOffsets)
        {
            return PartitionsAssignedHandler?.Invoke(
                this,
                partitionOffsets.Select(partitionOffset => partitionOffset.TopicPartition).ToList())?.ToList();
        }

        private List<TopicPartitionOffset>? InvokePartitionsRevokedHandler(string topicName)
        {
            if (PartitionsRevokedHandler == null || Assignment.Count == 0)
                return null;

            return PartitionsRevokedHandler.Invoke(
                    this,
                    Assignment.Where(topicPartition => topicPartition.Topic == topicName).Select(
                        partition => new TopicPartitionOffset(
                            partition,
                            _topics[partition.Topic].GetCommittedOffset(partition.Partition, GroupId))).ToList())
                ?.ToList();
        }

        private void ClearPartitionsAssignment()
        {
            Assignment.ToList().ForEach(topicPartition => Assignment.Remove(topicPartition));

            _currentOffsets.Clear();
            _storedOffsets.Clear();
        }

        private bool TryConsume(CancellationToken cancellationToken, out ConsumeResult<byte[]?, byte[]?>? result)
        {
            // Prevent consuming while rebalancing
            if (!PartitionsAssigned)
            {
                result = null;
                return false;
            }

            // Process the topics starting from the one that consumed less messages
            var topicPairs = _currentOffsets
                .OrderBy(topicPair => topicPair.Value.Sum(partitionPair => partitionPair.Value.Value));

            cancellationToken.ThrowIfCancellationRequested();

            foreach (var topicPair in topicPairs)
            {
                bool pulled = _topics[topicPair.Key].TryPull(
                    GroupId,
                    topicPair.Value.Select(
                            partitionPair => new TopicPartitionOffset(
                                topicPair.Key,
                                partitionPair.Key,
                                partitionPair.Value))
                        .ToList(),
                    out result);

                cancellationToken.ThrowIfCancellationRequested();

                if (pulled && Assignment.Contains(result!.TopicPartition))
                {
                    _currentOffsets[result.Topic][result.Partition] = result.Offset + 1;

                    return true;
                }
            }

            result = null;
            return false;
        }

        private TopicPartitionOffset GetStartingOffset(TopicPartitionOffset topicPartitionOffset)
        {
            if (!topicPartitionOffset.Offset.IsSpecial)
                return topicPartitionOffset;

            var topic = _topics[topicPartitionOffset.Topic];

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

                await Task.Delay(_config.AutoCommitIntervalMs ?? 5000).ConfigureAwait(false);
            }
        }
    }
}
