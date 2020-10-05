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
                Task.Run(AutoCommit);
        }

        /// <inheritdoc cref="IConsumer{TKey,TValue}.Handle" />
        public Handle Handle => throw new NotSupportedException();

        /// <inheritdoc cref="IConsumer{TKey,TValue}.Name" />
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
        ///     Gets a value indicating whether this instance was disposed.
        /// </summary>
        public bool Disposed { get; private set; }

        /// <inheritdoc cref="IConsumer{TKey,TValue}.MemberId" />
        public int AddBrokers(string brokers) => throw new NotSupportedException();

        /// <inheritdoc cref="IConsumer{TKey,TValue}.Consume(int)" />
        public ConsumeResult<byte[]?, byte[]?> Consume(int millisecondsTimeout) => throw new NotSupportedException();

        /// <inheritdoc cref="IConsumer{TKey,TValue}.Consume(CancellationToken)" />
        [SuppressMessage("", "CA2000", Justification = Justifications.NewUsingSyntaxFalsePositive)]
        public ConsumeResult<byte[]?, byte[]?> Consume(CancellationToken cancellationToken = default)
        {
            using var localCancellationTokenSource = new CancellationTokenSource();
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                localCancellationTokenSource.Token);

            // Process the topics starting from the one that consumed less messages
            var topicPairs = _currentOffsets
                .OrderBy(topicPair => topicPair.Value.Sum(partitionPair => partitionPair.Value.Value));

            var tasks = new List<Task<ConsumeResult<byte[]?, byte[]?>>>();

            foreach (var topicPair in topicPairs)
            {
                var task = Task.Run(
                    () => _topics[topicPair.Key].Pull(
                        GroupId,
                        topicPair.Value.Select(
                                partitionPair => new TopicPartitionOffset(
                                    topicPair.Key,
                                    partitionPair.Key,
                                    partitionPair.Value))
                            .ToList(),
                        linkedTokenSource.Token));

                tasks.Add(task);

                task.Wait(10, linkedTokenSource.Token);

                if (task.IsCompleted)
                    break;
            }

            // ReSharper disable once CoVariantArrayConversion
            var completedTaskIndex = Task.WaitAny(tasks.ToArray(), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            localCancellationTokenSource.Cancel();

            var result = tasks[completedTaskIndex].Result;

            _currentOffsets[result.Topic][result.Partition] = result.Offset + 1;

            return result;
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

                foreach (var topic in topicsList)
                {
                    _topics[topic].Subscribe(this);
                    Subscription.Add(topic);
                }
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

            _storedOffsets[offset.Topic][offset.Partition] = offset.Offset;
        }

        /// <inheritdoc cref="IConsumer{TKey,TValue}.Commit()" />
        public List<TopicPartitionOffset> Commit()
        {
            foreach (var topicPair in _storedOffsets)
            {
                _topics[topicPair.Key].Commit(
                    GroupId,
                    topicPair.Value.Select(
                        partitionPair => new TopicPartitionOffset(
                            topicPair.Key,
                            partitionPair.Key,
                            partitionPair.Value)));
            }

            return new List<TopicPartitionOffset>();
        }

        /// <inheritdoc cref="IConsumer{TKey,TValue}.Commit(IEnumerable{TopicPartitionOffset})" />
        public void Commit(IEnumerable<TopicPartitionOffset> offsets) => throw new NotSupportedException();

        /// <inheritdoc cref="IConsumer{TKey,TValue}.Commit(ConsumeResult{TKey,TValue})" />
        public void Commit(ConsumeResult<byte[]?, byte[]?> result) => throw new NotSupportedException();

        /// <inheritdoc cref="IConsumer{TKey,TValue}.Seek(TopicPartitionOffset)" />
        public void Seek(TopicPartitionOffset tpo)
        {
            Check.NotNull(tpo, nameof(tpo));

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
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose() => Disposed = true;

        internal void OnPartitionsAssigned(string topicName, IReadOnlyCollection<Partition> partitions)
        {
            // TODO: Invoke revoked event handler

            Assignment.Clear();

            _currentOffsets[topicName] = new ConcurrentDictionary<Partition, Offset>();
            _storedOffsets[topicName] = new ConcurrentDictionary<Partition, Offset>();

            foreach (var partition in partitions)
            {
                var offset = GetStartingOffset(topicName, partition);
                Assignment.Add(new TopicPartition(topicName, partition));
                Seek(offset);
            }
        }

        private TopicPartitionOffset GetStartingOffset(string topicName, Partition partition)
        {
            var topic = _topics[topicName];
            var offset = topic.GetCommittedOffset(partition, GroupId);

            // TODO: Invoke assigned event handler

            if (offset == Offset.End)
                offset = topic.GetLatestOffset(partition) + 1;
            else
                offset = 0;

            return new TopicPartitionOffset(topicName, partition, offset);
        }

        private async Task AutoCommit()
        {
            while (!Disposed)
            {
                Commit();
                await Task.Delay(_config.AutoCommitIntervalMs ?? 5000).ConfigureAwait(false);
            }
        }
    }
}
