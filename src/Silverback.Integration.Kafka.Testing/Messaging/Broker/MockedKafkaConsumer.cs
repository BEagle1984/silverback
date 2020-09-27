// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Silverback.Messaging.Broker.Topics;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    public class MockedKafkaConsumer : IConsumer<byte[]?, byte[]?>
    {
        private readonly ConsumerConfig _config;

        private readonly IInMemoryTopicCollection _topics;

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<Partition, Offset>> _currentOffsets =
            new ConcurrentDictionary<string, ConcurrentDictionary<Partition, Offset>>();

        private readonly ConcurrentDictionary<string, ConcurrentDictionary<Partition, Offset>> _storedOffsets =
            new ConcurrentDictionary<string, ConcurrentDictionary<Partition, Offset>>();

        private bool _disposed;

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

        public Handle Handle => throw new NotSupportedException();

        public string Name { get; }

        public string GroupId { get; }

        public string MemberId { get; }

        public List<TopicPartition> Assignment { get; } = new List<TopicPartition>();

        public List<string> Subscription { get; } = new List<string>();

        public IConsumerGroupMetadata ConsumerGroupMetadata => throw new NotSupportedException();

        public int AddBrokers(string brokers)
        {
            throw new NotSupportedException();
        }

        public ConsumeResult<byte[]?, byte[]?> Consume(int millisecondsTimeout)
        {
            throw new NotSupportedException();
        }

        public ConsumeResult<byte[]?, byte[]?> Consume(CancellationToken cancellationToken = default)
        {
            var topicPair = _currentOffsets.Single(); // TODO: Support multiple topics per consumer

            var result = _topics.GetTopic(topicPair.Key).Pull(
                GroupId,
                topicPair.Value.Select(
                        partitionPair => new TopicPartitionOffset(
                            topicPair.Key,
                            partitionPair.Key,
                            partitionPair.Value))
                    .ToList(),
                cancellationToken);

            _currentOffsets[result.Topic][result.Partition] = result.Offset + 1;

            return result;
        }

        public ConsumeResult<byte[]?, byte[]?> Consume(TimeSpan timeout)
        {
            throw new NotSupportedException();
        }

        public void Subscribe(IEnumerable<string> topics)
        {
            var topicsList = Check.NotNull(topics, nameof(topics)).AsReadOnlyList();
            Check.HasNoNullsOrEmpties(topicsList, nameof(topics));

            lock (Subscription)
            {
                Subscription.Clear();

                foreach (var topic in topicsList)
                {
                    _topics.GetTopic(topic).Subscribe(this);
                    Subscription.Add(topic);
                }
            }
        }

        public void Subscribe(string topic) => Subscribe(new[] { topic });

        public void Unsubscribe()
        {
            lock (Subscription)
            {
                foreach (var topic in Subscription)
                {
                    _topics.GetTopic(topic).Unsubscribe(this);
                }

                Subscription.Clear();
            }
        }

        public void Assign(TopicPartition partition)
        {
            throw new NotSupportedException();
        }

        public void Assign(TopicPartitionOffset partition)
        {
            throw new NotSupportedException();
        }

        public void Assign(IEnumerable<TopicPartitionOffset> partitions)
        {
            throw new NotSupportedException();
        }

        public void Assign(IEnumerable<TopicPartition> partitions)
        {
            throw new NotSupportedException();
        }

        public void Unassign()
        {
            throw new NotSupportedException();
        }

        public void StoreOffset(ConsumeResult<byte[]?, byte[]?> result)
        {
            if (result == null)
                return;

            StoreOffset(result.TopicPartitionOffset);
        }

        public void StoreOffset(TopicPartitionOffset offset)
        {
            if (offset == null)
                return;

            _storedOffsets[offset.Topic][offset.Partition] = offset.Offset;
        }

        public List<TopicPartitionOffset> Commit()
        {
            foreach (var topicPair in _storedOffsets)
            {
                _topics.GetTopic(topicPair.Key).Commit(
                    GroupId,
                    topicPair.Value.Select(
                        partitionPair => new TopicPartitionOffset(
                            topicPair.Key,
                            partitionPair.Key,
                            partitionPair.Value)));
            }

            return new List<TopicPartitionOffset>();
        }

        public void Commit(IEnumerable<TopicPartitionOffset> offsets)
        {
            throw new NotSupportedException();
        }

        public void Commit(ConsumeResult<byte[]?, byte[]?> result)
        {
            throw new NotSupportedException();
        }

        public void Seek(TopicPartitionOffset tpo)
        {
            Check.NotNull(tpo, nameof(tpo));

            _currentOffsets[tpo.Topic][tpo.Partition] = tpo.Offset;
        }

        public void Pause(IEnumerable<TopicPartition> partitions)
        {
            throw new NotSupportedException();
        }

        public void Resume(IEnumerable<TopicPartition> partitions)
        {
            throw new NotSupportedException();
        }

        public List<TopicPartitionOffset> Committed(TimeSpan timeout)
        {
            throw new NotSupportedException();
        }

        public List<TopicPartitionOffset> Committed(IEnumerable<TopicPartition> partitions, TimeSpan timeout)
        {
            throw new NotSupportedException();
        }

        public Offset Position(TopicPartition partition)
        {
            throw new NotSupportedException();
        }

        public List<TopicPartitionOffset> OffsetsForTimes(
            IEnumerable<TopicPartitionTimestamp> timestampsToSearch,
            TimeSpan timeout)
        {
            throw new NotSupportedException();
        }

        public WatermarkOffsets GetWatermarkOffsets(TopicPartition topicPartition)
        {
            throw new NotSupportedException();
        }

        public WatermarkOffsets QueryWatermarkOffsets(TopicPartition topicPartition, TimeSpan timeout)
        {
            throw new NotSupportedException();
        }

        public void Close()
        {
        }

        public void Dispose()
        {
            _disposed = true;
        }

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
            var topic = _topics.GetTopic(topicName);
            var offset = topic.GetCommittedOffset(partition, GroupId);

            // TODO: Invoke assigned event handler

            if (offset == Offset.End)
                offset = topic.GetLastOffset(partition) + 1;
            else
                offset = 0;

            return new TopicPartitionOffset(topicName, partition, offset);
        }

        private async Task AutoCommit()
        {
            while (!_disposed)
            {
                try
                {
                    Commit();
                    await Task.Delay(_config.AutoCommitIntervalMs ?? 5000).ConfigureAwait(false);
                }
                catch
                {
                    // Ignore
                }
            }
        }
    }
}
