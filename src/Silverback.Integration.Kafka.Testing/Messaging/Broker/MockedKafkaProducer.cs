// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Silverback.Messaging.Broker.Topics;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    public class MockedKafkaProducer : IProducer<byte[]?, byte[]?>
    {
        private readonly ProducerConfig _config;

        private readonly IInMemoryTopicCollection _topics;

        public MockedKafkaProducer(ProducerConfig config, IInMemoryTopicCollection topics)
        {
            _config = Check.NotNull(config, nameof(config));
            _topics = Check.NotNull(topics, nameof(topics));

            Name = $"{config.ClientId ?? "mocked"}.{Guid.NewGuid():N}";
        }

        public Handle Handle => throw new NotSupportedException();

        public string Name { get; }

        public int AddBrokers(string brokers)
        {
            throw new NotSupportedException();
        }

        public Task<DeliveryResult<byte[]?, byte[]?>> ProduceAsync(
            string topic,
            Message<byte[]?, byte[]?> message,
            CancellationToken cancellationToken = default)
        {
            int partition = 0; // TODO: Implement proper partitioning!

            var offset = _topics.GetTopic(topic).Push(partition, message);

            return Task.FromResult(
                new DeliveryResult<byte[]?, byte[]?>
                {
                    Message = message,
                    Topic = topic,
                    Partition = new Partition(partition),
                    Offset = offset,
                    Timestamp = new Timestamp(DateTime.Now),
                    Status = PersistenceStatus.Persisted
                });
        }

        public Task<DeliveryResult<byte[]?, byte[]?>> ProduceAsync(
            TopicPartition topicPartition,
            Message<byte[]?, byte[]?> message,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public void Produce(
            string topic,
            Message<byte[]?, byte[]?> message,
            Action<DeliveryReport<byte[]?, byte[]?>>? deliveryHandler = null)
        {
            throw new NotSupportedException();
        }

        public void Produce(
            TopicPartition topicPartition,
            Message<byte[]?, byte[]?> message,
            Action<DeliveryReport<byte[]?, byte[]?>> deliveryHandler = null)
        {
            throw new NotSupportedException();
        }

        public int Poll(TimeSpan timeout)
        {
            throw new NotSupportedException();
        }

        public int Flush(TimeSpan timeout)
        {
            return 0;
        }

        public void Flush(CancellationToken cancellationToken = default)
        {
        }

        public void InitTransactions(TimeSpan timeout)
        {
            throw new NotSupportedException();
        }

        public void BeginTransaction()
        {
            throw new NotSupportedException();
        }

        public void CommitTransaction(TimeSpan timeout)
        {
            throw new NotSupportedException();
        }

        public void AbortTransaction(TimeSpan timeout)
        {
            throw new NotSupportedException();
        }

        public void SendOffsetsToTransaction(
            IEnumerable<TopicPartitionOffset> offsets,
            IConsumerGroupMetadata groupMetadata,
            TimeSpan timeout)
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
        }
    }
}
