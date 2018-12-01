using Silverback.Messaging.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Confluent.Kafka.Serialization;
using Microsoft.Extensions.Logging;

namespace Silverback.Messaging.Broker
{
    public class KafkaProducer : Producer<KafkaBroker, KafkaProducerEndpoint>, IDisposable
    {
        private readonly ILogger _logger;
        private Confluent.Kafka.Producer<byte[], byte[]> _innerProducer;

        private static readonly ConcurrentDictionary<Dictionary<string, object>, Confluent.Kafka.Producer<byte[], byte[]>> ProducersCache =
            new ConcurrentDictionary<Dictionary<string, object>, Confluent.Kafka.Producer<byte[], byte[]>>(new KafkaConfigurationComparer());

        public KafkaProducer(KafkaBroker broker, KafkaProducerEndpoint endpoint, ILogger<KafkaProducer> logger) : base(broker, endpoint, logger)
        {
            _logger = logger;
        }

        protected override void Produce(IMessage message, byte[] serializedMessage)
            => Task.Run(() => ProduceAsync(message, serializedMessage)).Wait();

        protected override async Task ProduceAsync(IMessage message, byte[] serializedMessage)
        {
            var msg = await GetInnerProducer().ProduceAsync(Endpoint.Name, KeyHelper.GetMessageKey(message), serializedMessage);
            if (msg.Error.HasError) throw new SilverbackException($"Failed to produce message: [{msg.Error.Code}] {msg.Error.Reason}");
        }

        private Confluent.Kafka.Producer<byte[], byte[]> GetInnerProducer() =>
            _innerProducer ?? (_innerProducer =
                ProducersCache.GetOrAdd(Endpoint.Configuration, CreateInnerProducer()));

        private Confluent.Kafka.Producer<byte[], byte[]> CreateInnerProducer()
        {
            _logger.LogTrace("Creating Confluent.Kafka.Producer...");

            return new Confluent.Kafka.Producer<byte[], byte[]>(Endpoint.Configuration, new ByteArraySerializer(),
                new ByteArraySerializer());
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
