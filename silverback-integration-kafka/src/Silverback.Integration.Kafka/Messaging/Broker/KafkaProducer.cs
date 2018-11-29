using Silverback.Messaging.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Confluent.Kafka.Serialization;
using Microsoft.Extensions.Logging;

namespace Silverback.Messaging.Broker
{
    public class KafkaProducer : Producer, IDisposable
    {
        private Confluent.Kafka.Producer<byte[], byte[]> _innerProducer;

        private static readonly ConcurrentDictionary<Dictionary<string, object>, Confluent.Kafka.Producer<byte[], byte[]>> ProducersCache =
            new ConcurrentDictionary<Dictionary<string, object>, Confluent.Kafka.Producer<byte[], byte[]>>(new ConfigurationComparer());

        public KafkaProducer(IBroker broker, KafkaEndpoint endpoint, ILogger<KafkaProducer> logger) : base(broker, endpoint, logger)
        {
        }

        public new KafkaEndpoint Endpoint => (KafkaEndpoint)base.Endpoint;

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

        private Confluent.Kafka.Producer<byte[], byte[]> CreateInnerProducer() =>
            new Confluent.Kafka.Producer<byte[], byte[]>(Endpoint.Configuration, new ByteArraySerializer(), new ByteArraySerializer());

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
