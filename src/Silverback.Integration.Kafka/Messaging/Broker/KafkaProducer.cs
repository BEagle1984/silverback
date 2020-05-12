// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="Producer{TBroker,TEndpoint}" />
    public class KafkaProducer : Producer<KafkaBroker, KafkaProducerEndpoint>, IDisposable
    {
        private readonly KafkaEventsHandler _kafkaEventsHandler;
        private readonly ILogger _logger;

        private Confluent.Kafka.IProducer<byte[], byte[]> _innerProducer;

        private static readonly
            ConcurrentDictionary<Confluent.Kafka.ProducerConfig, Confluent.Kafka.IProducer<byte[], byte[]>>
            ProducersCache =
                new ConcurrentDictionary<Confluent.Kafka.ProducerConfig, Confluent.Kafka.IProducer<byte[], byte[]>>(
                    new ConfigurationDictionaryComparer<string, string>());

        public KafkaProducer(
            KafkaBroker broker,
            KafkaProducerEndpoint endpoint,
            IReadOnlyCollection<IProducerBehavior> behaviors,
            IServiceProvider serviceProvider,
            ILogger<KafkaProducer> logger)
            : base(broker, endpoint, behaviors, logger)
        {
            _logger = logger;

            _kafkaEventsHandler = serviceProvider.GetRequiredService<KafkaEventsHandler>();
        }

        protected override IOffset ProduceCore(IRawOutboundEnvelope envelope) =>
            AsyncHelper.RunSynchronously(() => ProduceAsyncCore(envelope));

        protected override async Task<IOffset> ProduceAsyncCore(IRawOutboundEnvelope envelope)
        {
            try
            {
                var kafkaMessage = new Confluent.Kafka.Message<byte[], byte[]>
                {
                    Key = GetKafkaKeyAndRemoveHeader(envelope.Headers),
                    Value = envelope.RawMessage
                };

                if (envelope.Headers != null && envelope.Headers.Any())
                {
                    kafkaMessage.Headers = envelope.Headers.ToConfluentHeaders();
                }

                var deliveryReport = await GetInnerProducer().ProduceAsync(Endpoint.Name, kafkaMessage);

                if (Endpoint.Configuration.ArePersistenceStatusReportsEnabled)
                {
                    CheckPersistenceStatus(deliveryReport);
                }

                return new KafkaOffset(deliveryReport.TopicPartitionOffset);
            }
            catch (Confluent.Kafka.KafkaException ex)
            {
                // Disposing and re-creating the producer will maybe fix the issue
                if (Endpoint.Configuration.DisposeOnException)
                    DisposeInnerProducer();

                throw new ProduceException(
                    "Error occurred producing the message. See inner exception for details.",
                    ex);
            }
        }

        private byte[] GetKafkaKeyAndRemoveHeader(MessageHeaderCollection headers)
        {
            var kafkaKeyHeader = headers
                ?.FirstOrDefault(header => header.Key == KafkaMessageHeaders.KafkaMessageKey);

            if (kafkaKeyHeader != null)
            {
                headers.Remove(kafkaKeyHeader);

                if (kafkaKeyHeader.Value != null)
                    return Endpoint.Serializer is IKafkaMessageSerializer kafkaSerializer
                        ? kafkaSerializer.SerializeKey(
                            kafkaKeyHeader.Value,
                            headers,
                            new MessageSerializationContext(Endpoint, Endpoint.Name))
                        : Encoding.UTF8.GetBytes(kafkaKeyHeader.Value);
            }

            return null;
        }

        private Confluent.Kafka.IProducer<byte[], byte[]> GetInnerProducer() =>
            _innerProducer ??=
                ProducersCache.GetOrAdd(Endpoint.Configuration.ConfluentConfig, _ => CreateInnerProducer());

        private Confluent.Kafka.IProducer<byte[], byte[]> CreateInnerProducer()
        {
            _logger.LogDebug(EventIds.KafkaProducerCreatingProducer, "Creating Confluent.Kafka.Producer...");

            var producerBuilder =
                new Confluent.Kafka.ProducerBuilder<byte[], byte[]>(Endpoint.Configuration.ConfluentConfig);

            _kafkaEventsHandler.SetProducerEventsHandlers(producerBuilder);

            return producerBuilder.Build();
        }

        private void CheckPersistenceStatus(Confluent.Kafka.DeliveryResult<byte[], byte[]> deliveryReport)
        {
            switch (deliveryReport.Status)
            {
                case Confluent.Kafka.PersistenceStatus.PossiblyPersisted
                    when Endpoint.Configuration.ThrowIfNotAcknowledged:
                {
                    throw new ProduceException(
                        "The message was transmitted to broker, but no acknowledgement was received.");
                }
                case Confluent.Kafka.PersistenceStatus.PossiblyPersisted:
                {
                    _logger.LogWarning(EventIds.KafkaProducerMessageTransmittedWithoutAcknowledgement, "The message was transmitted to broker, but no acknowledgement was received.");
                    break;
                }
                case Confluent.Kafka.PersistenceStatus.NotPersisted:
                {
                    throw new ProduceException("The message was never transmitted to the broker, " +
                                               "or failed with an error indicating it was not written " +
                                               "to the log.'");
                }
            }
        }

        private void DisposeInnerProducer()
        {
            // Dispose only if still in cache to avoid ObjectDisposedException
            if (!ProducersCache.TryRemove(Endpoint.Configuration.ConfluentConfig, out _))
                return;

            _innerProducer?.Flush(TimeSpan.FromSeconds(10));
            _innerProducer?.Dispose();
            _innerProducer = null;
        }

        public void Dispose()
        {
            DisposeInnerProducer();
        }
    }
}