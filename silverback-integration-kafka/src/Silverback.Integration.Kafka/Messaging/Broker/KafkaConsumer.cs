// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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
        private Dictionary<Confluent.Kafka.TopicPartition, Confluent.Kafka.Offset> _pendingOffsets = new Dictionary<Confluent.Kafka.TopicPartition, Confluent.Kafka.Offset>();

        public KafkaConsumer(KafkaBroker broker, KafkaConsumerEndpoint endpoint, ILogger<KafkaConsumer> logger) : base(broker, endpoint, logger)
        {
            _logger = logger;

            Endpoint.Validate();
        }

        internal void Connect()
        {
            if (_innerConsumer != null)
                return;

            _innerConsumer = new InnerConsumerWrapper(
                new Confluent.Kafka.Consumer<byte[], byte[]>(Endpoint.Configuration),
                _cancellationTokenSource.Token,
                _logger);

            _innerConsumer.Subscribe(Endpoint);
            _innerConsumer.Received += OnMessageReceived;
            _innerConsumer.StartConsuming();

            _logger.LogTrace("Connected consumer to topic {topic}. (BootstrapServers=\"{bootstrapServers}\")", Endpoint.Name, Endpoint.Configuration.BootstrapServers);
        }

        internal void Disconnect()
        {
            if (_innerConsumer == null)
                return;

            _cancellationTokenSource.Cancel();

            if (!Endpoint.Configuration.IsAutoCommitEnabled)
                _innerConsumer.CommitAll();

            _innerConsumer.Dispose();
            _innerConsumer = null;

            _logger.LogTrace("Disconnected consumer from topic {topic}. (BootstrapServers=\"{bootstrapServers}\")", Endpoint.Name, Endpoint.Configuration.BootstrapServers);
        }

        public void Dispose()
        {
            Disconnect();
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
                _messagesSinceCommit++;
                HandleMessage(message.Value, new KafkaOffset(tpo));
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

        public override void Acknowledge(IEnumerable<IOffset> offsets)
        {
            var lastOffsets = offsets.OfType<KafkaOffset>()
                .GroupBy(o => o.Key)
                .Select(g => g
                    .OrderByDescending(o => o.TopicPartitionOffset.Offset.Value)
                    .First()
                    .TopicPartitionOffset)
                .ToList();

            _innerConsumer.StoreOffset(lastOffsets
                .Select(o => new Confluent.Kafka.TopicPartitionOffset(o.TopicPartition, o.Offset + 1))
                .ToArray());

            if (!Endpoint.Configuration.IsAutoCommitEnabled)
                CommitOffsets(lastOffsets);
        }

        private void CommitOffsets(IEnumerable<Confluent.Kafka.TopicPartitionOffset> offsets)
        {
            if (++_messagesSinceCommit < Endpoint.Configuration.CommitOffsetEach) return;

            _innerConsumer.Commit(offsets);

            _messagesSinceCommit = 0;
        }
    }
}
