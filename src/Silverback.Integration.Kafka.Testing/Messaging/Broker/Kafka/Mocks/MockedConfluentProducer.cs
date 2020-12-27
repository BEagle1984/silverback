// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka.Mocks
{
    internal sealed class MockedConfluentProducer : IMockedConfluentProducer
    {
        private readonly IInMemoryTopicCollection _topics;

        public MockedConfluentProducer(ProducerConfig config, IInMemoryTopicCollection topics)
        {
            Check.NotNull(config, nameof(config));
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

            var inMemoryTopic = _topics[topicPartition.Topic];

            var partitionIndex =
                topicPartition.Partition == Partition.Any
                    ? GetPartitionIndex(inMemoryTopic, message.Key)
                    : topicPartition.Partition.Value;

            var offset = inMemoryTopic.Push(partitionIndex, message);

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

        public void Produce(
            TopicPartition topicPartition,
            Message<byte[]?, byte[]?> message,
            Action<DeliveryReport<byte[]?, byte[]?>>? deliveryHandler = null) =>
            throw new NotSupportedException();

        public int Poll(TimeSpan timeout) => throw new NotSupportedException();

        public int Flush(TimeSpan timeout) => 0;

        public void Flush(CancellationToken cancellationToken = default)
        {
            // Nothing to flush
        }

        public void InitTransactions(TimeSpan timeout) => throw new NotSupportedException();

        public void BeginTransaction() => throw new NotSupportedException();

        public void CommitTransaction(TimeSpan timeout) => throw new NotSupportedException();

        public void AbortTransaction(TimeSpan timeout) => throw new NotSupportedException();

        public void SendOffsetsToTransaction(
            IEnumerable<TopicPartitionOffset> offsets,
            IConsumerGroupMetadata groupMetadata,
            TimeSpan timeout)
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
            // Nothing to dispose, IDisposable is just inherited from IProducer but it's not needed in the mock
        }

        private static int GetPartitionIndex(IInMemoryTopic topic, byte[]? messageKey)
        {
            if (messageKey == null)
                return 0;

            return messageKey.Last() % topic.Partitions.Count;
        }
    }
}
