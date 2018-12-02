// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public class KafkaProducer : Producer<KafkaBroker, KafkaProducerEndpoint>, IDisposable
    {
        private readonly ILogger _logger;
        private Confluent.Kafka.Producer<byte[], byte[]> _innerProducer;

        private static readonly ConcurrentDictionary<Confluent.Kafka.ProducerConfig, Confluent.Kafka.Producer<byte[], byte[]>> ProducersCache =
            new ConcurrentDictionary<Confluent.Kafka.ProducerConfig, Confluent.Kafka.Producer<byte[], byte[]>>(new KafkaClientConfigComparer());

        public KafkaProducer(KafkaBroker broker, KafkaProducerEndpoint endpoint, ILogger<KafkaProducer> logger) : base(broker, endpoint, logger)
        {
            _logger = logger;
        }

        protected override void Produce(IMessage message, byte[] serializedMessage)
            => Task.Run(() => ProduceAsync(message, serializedMessage)).Wait();

        protected override async Task ProduceAsync(IMessage message, byte[] serializedMessage)
        {
            var kafkaMessage = new Confluent.Kafka.Message<byte[], byte[]>
            {
                Key = KeyHelper.GetMessageKey(message),
                Value = serializedMessage
            };

            var deliveryReport = await GetInnerProducer().ProduceAsync(Endpoint.Name, kafkaMessage);
            _logger.LogTrace(
                "Successfully produced: {topic} [{partition}] @{offset}.",
                deliveryReport.Topic, deliveryReport.Partition, deliveryReport.Offset);
        }

        private Confluent.Kafka.Producer<byte[], byte[]> GetInnerProducer() =>
            _innerProducer ?? (_innerProducer =
                ProducersCache.GetOrAdd(Endpoint.Configuration, CreateInnerProducer()));

        private Confluent.Kafka.Producer<byte[], byte[]> CreateInnerProducer()
        {
            _logger.LogTrace("Creating Confluent.Kafka.Producer...");

            return new Confluent.Kafka.Producer<byte[], byte[]>(Endpoint.Configuration);
        }

        public void Dispose()
        {
            // Dispose only if still in cache to avoid ObjectDisposedException
            if (!ProducersCache.TryRemove(Endpoint.Configuration, out var _))
                return;

            _innerProducer?.Dispose();
            _innerProducer = null;
        }
    }
}
