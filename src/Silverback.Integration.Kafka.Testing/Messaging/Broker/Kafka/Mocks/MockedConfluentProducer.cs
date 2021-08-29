// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka.Mocks
{
    internal sealed class MockedConfluentProducer : IMockedConfluentProducer
    {
        private readonly ProducerConfig _config;

        private readonly IInMemoryTopicCollection _topics;

        private readonly object _roundRobinLockObject = new();

        private int _lastPushedPartition = -1;

        public MockedConfluentProducer(ProducerConfig config, IInMemoryTopicCollection topics)
        {
            _config = Check.NotNull(config, nameof(config));
            _topics = Check.NotNull(topics, nameof(topics));

            Name = $"{config.ClientId ?? "mocked"}.{Guid.NewGuid():N}";
        }

        public Handle Handle => throw new NotSupportedException();

        public string Name { get; }

        internal Action<IProducer<byte[]?, byte[]?>, string>? StatisticsHandler { get; set; }

        public int AddBrokers(string brokers) => throw new NotSupportedException();

        public Task<DeliveryResult<byte[]?, byte[]?>> ProduceAsync(
            string topic,
            Message<byte[]?, byte[]?> message,
            CancellationToken cancellationToken = default) =>
            ProduceAsync(new TopicPartition(topic, Partition.Any), message, cancellationToken);

        public Task<DeliveryResult<byte[]?, byte[]?>> ProduceAsync(
            TopicPartition topicPartition,
            Message<byte[]?, byte[]?> message,
            CancellationToken cancellationToken = default)
        {
            Check.NotNull(message, nameof(message));

            int partitionIndex = PushToTopic(topicPartition, message, out Offset offset);

            return Task.FromResult(
                new DeliveryResult<byte[]?, byte[]?>
                {
                    Message = message,
                    Topic = topicPartition.Topic,
                    Partition = new Partition(partitionIndex),
                    Offset = offset,
                    Timestamp = new Timestamp(DateTime.Now),
                    Status = PersistenceStatus.Persisted
                });
        }

        public void Produce(
            string topic,
            Message<byte[]?, byte[]?> message,
            Action<DeliveryReport<byte[]?, byte[]?>>? deliveryHandler = null) =>
            throw new NotSupportedException();

        [SuppressMessage("", "CA1031", Justification = "Exception forwarded to callback")]
        public void Produce(
            TopicPartition topicPartition,
            Message<byte[]?, byte[]?> message,
            Action<DeliveryReport<byte[]?, byte[]?>>? deliveryHandler = null)
        {
            Check.NotNull(message, nameof(message));

            try
            {
                var partitionIndex = PushToTopic(topicPartition, message, out Offset offset);

                Task.Run(
                        async () =>
                        {
                            await Task.Delay(10).ConfigureAwait(false);

                            deliveryHandler?.Invoke(
                                new DeliveryReport<byte[]?, byte[]?>
                                {
                                    Message = message,
                                    Error = new Error(ErrorCode.NoError),
                                    Topic = topicPartition.Topic,
                                    Partition = new Partition(partitionIndex),
                                    Offset = offset,
                                    Timestamp = new Timestamp(DateTime.Now),
                                    Status = PersistenceStatus.Persisted
                                });
                        })
                    .FireAndForget();
            }
            catch (Exception ex)
            {
                deliveryHandler?.Invoke(
                    new DeliveryReport<byte[]?, byte[]?>
                    {
                        Message = message,
                        Error = new Error(ErrorCode.Unknown, ex.ToString(), false),
                        Topic = topicPartition.Topic,
                        Status = PersistenceStatus.NotPersisted
                    });
            }
        }

        public int Poll(TimeSpan timeout) => throw new NotSupportedException();

        public int Flush(TimeSpan timeout) => 0;

        public void Flush(CancellationToken cancellationToken = default)
        {
            // Nothing to flush
        }

        public void InitTransactions(TimeSpan timeout) => throw new NotSupportedException();

        public void BeginTransaction() => throw new NotSupportedException();

        public void CommitTransaction(TimeSpan timeout) => throw new NotSupportedException();

        public void CommitTransaction() => throw new NotSupportedException();

        public void AbortTransaction(TimeSpan timeout) => throw new NotSupportedException();

        public void AbortTransaction() => throw new NotSupportedException();

        public void SendOffsetsToTransaction(
            IEnumerable<TopicPartitionOffset> offsets,
            IConsumerGroupMetadata groupMetadata,
            TimeSpan timeout) =>
            throw new NotSupportedException();

        public void Dispose()
        {
            // Nothing to dispose, IDisposable is just inherited from IProducer but it's not needed in the mock
        }

        [SuppressMessage("", "CA5394", Justification = "Usecure randomness is fine here")]
        private int GetPartitionIndex(IInMemoryTopic topic, byte[]? messageKey)
        {
            if (messageKey == null)
                return GetNextRoundRobinPartition(topic);

            return messageKey.Last() % topic.Partitions.Count;
        }

        private int PushToTopic(
            TopicPartition topicPartition,
            Message<byte[]?, byte[]?> message,
            out Offset offset)
        {
            var inMemoryTopic = _topics.Get(topicPartition.Topic, _config);

            var partitionIndex =
                topicPartition.Partition == Partition.Any
                    ? GetPartitionIndex(inMemoryTopic, message.Key)
                    : topicPartition.Partition.Value;

            offset = inMemoryTopic.Push(partitionIndex, message);
            return partitionIndex;
        }

        private int GetNextRoundRobinPartition(IInMemoryTopic topic)
        {
            lock (_roundRobinLockObject)
            {
                if (++_lastPushedPartition >= topic.Partitions.Count)
                    _lastPushedPartition = 0;

                return _lastPushedPartition;
            }
        }
    }
}
