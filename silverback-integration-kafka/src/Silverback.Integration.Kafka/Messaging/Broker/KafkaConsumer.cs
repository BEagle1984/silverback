// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Silverback.Messaging.Broker
{
    public class KafkaConsumer : Consumer<KafkaBroker, KafkaConsumerEndpoint>, IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly ILogger<KafkaConsumer> _logger;

        private InnerConsumerWrapper _innerConsumer;
        private int _messagesSinceCommit = 0;
        private int _currentBatchSize = 0;
        private Dictionary<Confluent.Kafka.TopicPartition, Confluent.Kafka.Offset> _pendingOffsets = new Dictionary<Confluent.Kafka.TopicPartition, Confluent.Kafka.Offset>();

        private static readonly ConcurrentDictionary<Confluent.Kafka.ConsumerConfig, InnerConsumerWrapper> ConsumerWrappersCache =
            new ConcurrentDictionary<Confluent.Kafka.ConsumerConfig, InnerConsumerWrapper>(new KafkaClientConfigComparer());

        public KafkaConsumer(KafkaBroker broker, KafkaConsumerEndpoint endpoint, ILogger<KafkaConsumer> logger) : base(broker, endpoint, logger)
        {
            _logger = logger;
        }

        internal void Connect()
        {
            if (_innerConsumer != null)
                return;

            Endpoint.Validate();

            _innerConsumer = Endpoint.ReuseConsumer 
                ? ConsumerWrappersCache.GetOrAdd(Endpoint.Configuration, _ => CreateInnerConsumerWrapper()) 
                : CreateInnerConsumerWrapper();

            _innerConsumer.Subscribe(Endpoint);
            _innerConsumer.Received += (message, tpo) => OnMessageReceived(message, tpo, _innerConsumer);
            _innerConsumer.StartConsuming();

            _logger.LogTrace("Connected consumer to topic {topic}. (BootstrapServers=\"{bootstrapServers}\")", Endpoint.Name, Endpoint.Configuration.BootstrapServers);
        }

        internal void Disconnect()
        {
            if (_innerConsumer == null)
                return;

            // Remove from cache but take in account that it may have been removed already by another 
            // consumer sharing the same instance.
            ConsumerWrappersCache.TryRemove(Endpoint.Configuration, out var _);

            _cancellationTokenSource.Cancel();

            if (!Endpoint.IsAutoCommitEnabled)
                _innerConsumer.CommitAll();

            _innerConsumer.Dispose();
            _innerConsumer = null;

            _logger.LogTrace("Disconnected consumer from topic {topic}. (BootstrapServers=\"{bootstrapServers}\")", Endpoint.Name, Endpoint.Configuration.BootstrapServers);
        }

        public void Dispose()
        {
            Disconnect();
        }

        private InnerConsumerWrapper CreateInnerConsumerWrapper(int threadIndex = 1)
        {
            _logger.LogTrace("Creating Confluent.Kafka.Consumer...");

            var configuration = GetCurrentThreadConfiguration(threadIndex);

            return new InnerConsumerWrapper(
                new Confluent.Kafka.Consumer<byte[], byte[]>(configuration),
                _cancellationTokenSource.Token,
                _logger);
        }

        private IEnumerable<KeyValuePair<string, string>> GetCurrentThreadConfiguration(int threadIndex)
        {
            IEnumerable<KeyValuePair<string, string>> configuration;

            if (Endpoint.ConsumerThreads > 1)
            {
                var dict = Endpoint.Configuration.ToDictionary(d => d.Key, d => d.Value);
                dict["client.id"] = $"{dict["client.id"]}[{threadIndex}]";
                configuration = dict;
            }
            else
            {
                configuration = Endpoint.Configuration;
            }

            return configuration;
        }

        private void OnMessageReceived(Confluent.Kafka.Message<byte[], byte[]> message, Confluent.Kafka.TopicPartitionOffset tpo)
        {
            // Checking if the message was sent to the subscribed topic is necessary
            // when reusing the same consumer for multiple topics.
            if (!tpo.Topic.Equals(Endpoint.Name, StringComparison.InvariantCultureIgnoreCase))
                return;

            TryHandleMessage(message, tpo);
        }

        private void TryHandleMessage(Confluent.Kafka.Message<byte[], byte[]> message, Confluent.Kafka.TopicPartitionOffset tpo)
        {
            try
            {
                HandleMessage(message.Value);

                StoreOffset(tpo);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex,
                    "Fatal error occurred consuming the message: {topic} {partition} @{offset}. " +
                    "The consumer will be stopped.",
                    tpo.Topic, tpo.Partition, tpo.Offset);

                _cancellationTokenSource.Cancel();
            }
        }

        private void StoreOffset(Confluent.Kafka.TopicPartitionOffset tpo)
        {
            _pendingOffsets[tpo.TopicPartition] = tpo.Offset;

            // Store offset in inner consumer only if batch is completely processed
            if (Endpoint.Batch.Size > 1 && ++_currentBatchSize < Endpoint.Batch.Size)
                return;

            _innerConsumer.StoreOffset(_pendingOffsets
                .Select(o => new Confluent.Kafka.TopicPartitionOffset(o.Key, o.Value + 1))
                .ToArray());

            _currentBatchSize = 0;

            CommitOffsets();
        }

        private void CommitOffsets()
        {
            if (Endpoint.IsAutoCommitEnabled) return;
            if (++_messagesSinceCommit % Endpoint.CommitOffsetEach != 0) return;

            _innerConsumer.Commit(_pendingOffsets
                .Select(o => new Confluent.Kafka.TopicPartitionOffset(o.Key, o.Value))
                .ToArray());

            _messagesSinceCommit = 0;
        }
    }
}
