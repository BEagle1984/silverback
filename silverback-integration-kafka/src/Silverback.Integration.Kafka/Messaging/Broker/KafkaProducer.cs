// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    public class KafkaProducer : Producer<KafkaBroker, KafkaProducerEndpoint>, IDisposable
    {
        private readonly ILogger _logger;
        private Confluent.Kafka.Producer<byte[], byte[]> _innerProducer;

        private static readonly ConcurrentDictionary<Confluent.Kafka.ProducerConfig, Confluent.Kafka.Producer<byte[], byte[]>> ProducersCache =
            new ConcurrentDictionary<Confluent.Kafka.ProducerConfig, Confluent.Kafka.Producer<byte[], byte[]>>(new KafkaClientConfigComparer());

        public KafkaProducer(KafkaBroker broker, KafkaProducerEndpoint endpoint, MessageKeyProvider messageKeyProvider, 
            ILogger<KafkaProducer> logger, MessageLogger messageLogger)
            : base(broker, endpoint, messageKeyProvider, logger, messageLogger)
        {
            _logger = logger;

            Endpoint.Validate();
        }

        protected override void Produce(object message, byte[] serializedMessage, IEnumerable<MessageHeader> headers) => 
            Task.Run(() => ProduceAsync(message, serializedMessage, headers)).Wait();

        protected override async Task ProduceAsync(object message, byte[] serializedMessage, IEnumerable<MessageHeader> headers)
        {
            var kafkaMessage = new Confluent.Kafka.Message<byte[], byte[]>
            {
                Key = KeyHelper.GetMessageKey(message),
                Value = serializedMessage
            };

            if (headers != null && headers.Any())
            {
                kafkaMessage.Headers = new Confluent.Kafka.Headers();
                headers.ForEach(h => kafkaMessage.Headers.Add(h.ToConfluentHeader()));
            }

            var deliveryReport = await GetInnerProducer().ProduceAsync(Endpoint.Name, kafkaMessage);
            _logger.LogTrace(
                "Successfully produced: {topic} {partition} @{offset}.",
                deliveryReport.Topic, deliveryReport.Partition, deliveryReport.Offset);
        }

        private Confluent.Kafka.Producer<byte[], byte[]> GetInnerProducer() =>
            _innerProducer ?? (_innerProducer =
                ProducersCache.GetOrAdd(Endpoint.Configuration.ConfluentConfig, _ => CreateInnerProducer()));

        private Confluent.Kafka.Producer<byte[], byte[]> CreateInnerProducer()
        {
            _logger.LogTrace("Creating Confluent.Kafka.Producer...");

            return new Confluent.Kafka.Producer<byte[], byte[]>(Endpoint.Configuration.ConfluentConfig);
        }

        public void Dispose()
        {
            // Dispose only if still in cache to avoid ObjectDisposedException
            if (!ProducersCache.TryRemove(Endpoint.Configuration.ConfluentConfig, out _))
                return;

            _innerProducer?.Flush(TimeSpan.FromSeconds(10));
            _innerProducer?.Dispose();
            _innerProducer = null;
        }
    }
}
