// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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
    ///     A mocked implementation of <see cref="IProducer{TKey,TValue}" /> from Confluent.Kafka that produces
    ///     to an <see cref="InMemoryTopic" />.
    /// </summary>
    [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
    public sealed class MockedKafkaProducer : IProducer<byte[]?, byte[]?>
    {
        private readonly IInMemoryTopicCollection _topics;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockedKafkaProducer"/> class.
        /// </summary>
        /// <param name="config">
        ///     The producer configuration.
        /// </param>
        /// <param name="topics">
        ///     The collection of <see cref="InMemoryTopic" />.
        /// </param>
        public MockedKafkaProducer(ProducerConfig config, IInMemoryTopicCollection topics)
        {
            Check.NotNull(config, nameof(config));
            _topics = Check.NotNull(topics, nameof(topics));

            Name = $"{config.ClientId ?? "mocked"}.{Guid.NewGuid():N}";
        }

        /// <inheritdoc cref="IClient.Handle" />
        public Handle Handle => throw new NotSupportedException();

        /// <inheritdoc cref="IClient.Name" />
        public string Name { get; }

        internal Action<IProducer<byte[]?, byte[]?>, string>? StatisticsHandler { get; set; }

        /// <inheritdoc cref="IClient.AddBrokers" />
        public int AddBrokers(string brokers) => throw new NotSupportedException();

        /// <inheritdoc cref="IProducer{TKey,TValue}.ProduceAsync(string,Message{TKey,TValue},CancellationToken)" />
        public Task<DeliveryResult<byte[]?, byte[]?>> ProduceAsync(
            string topic,
            Message<byte[]?, byte[]?> message,
            CancellationToken cancellationToken = default)
        {
            Check.NotNull(message, nameof(message));

            var inMemoryTopic = _topics[topic];
            int partition = GetPartitionIndex(inMemoryTopic, message.Key);

            var offset = inMemoryTopic.Push(partition, message);

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

        /// <inheritdoc cref="IProducer{TKey,TValue}.ProduceAsync(TopicPartition,Message{TKey,TValue},CancellationToken)" />
        public Task<DeliveryResult<byte[]?, byte[]?>> ProduceAsync(
            TopicPartition topicPartition,
            Message<byte[]?, byte[]?> message,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        /// <inheritdoc cref="IProducer{TKey,TValue}.Produce(string,Message{TKey,TValue},Action{DeliveryReport{TKey,TValue}})" />
        public void Produce(
            string topic,
            Message<byte[]?, byte[]?> message,
            Action<DeliveryReport<byte[]?, byte[]?>>? deliveryHandler = null) =>
            throw new NotSupportedException();

        /// <inheritdoc cref="IProducer{TKey,TValue}.Produce(TopicPartition,Message{TKey,TValue},Action{DeliveryReport{TKey,TValue}})" />
        public void Produce(
            TopicPartition topicPartition,
            Message<byte[]?, byte[]?> message,
            Action<DeliveryReport<byte[]?, byte[]?>>? deliveryHandler = null) =>
            throw new NotSupportedException();

        /// <inheritdoc cref="IProducer{TKey,TValue}.Poll" />
        public int Poll(TimeSpan timeout) => throw new NotSupportedException();

        /// <inheritdoc cref="IProducer{TKey,TValue}.Flush(TimeSpan)" />
        public int Flush(TimeSpan timeout) => 0;

        /// <inheritdoc cref="IProducer{TKey,TValue}.Flush(CancellationToken)" />
        public void Flush(CancellationToken cancellationToken = default)
        {
            // Nothing to flush
        }

        /// <inheritdoc cref="IProducer{TKey,TValue}.InitTransactions" />
        public void InitTransactions(TimeSpan timeout) => throw new NotSupportedException();

        /// <inheritdoc cref="IProducer{TKey,TValue}.BeginTransaction" />
        public void BeginTransaction() => throw new NotSupportedException();

        /// <inheritdoc cref="IProducer{TKey,TValue}.CommitTransaction" />
        public void CommitTransaction(TimeSpan timeout) => throw new NotSupportedException();

        /// <inheritdoc cref="IProducer{TKey,TValue}.AbortTransaction" />
        public void AbortTransaction(TimeSpan timeout) => throw new NotSupportedException();

        /// <inheritdoc cref="IProducer{TKey,TValue}.SendOffsetsToTransaction" />
        public void SendOffsetsToTransaction(
            IEnumerable<TopicPartitionOffset> offsets,
            IConsumerGroupMetadata groupMetadata,
            TimeSpan timeout)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            // Nothing to dispose, IDisposable is just inherited from IProducer but it's not needed in the mock
        }

        private static int GetPartitionIndex(IInMemoryTopic topic, byte[]? messageKey)
        {
            if (messageKey == null)
                return 0;

            return messageKey.Last() % topic.PartitionsCount;
        }
    }
}
