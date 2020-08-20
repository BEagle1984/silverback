// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
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
    public sealed class KafkaProducer : Producer<KafkaBroker, KafkaProducerEndpoint>, IDisposable
    {
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        private static readonly
            ConcurrentDictionary<ProducerConfig, IProducer<byte[]?, byte[]?>>
            ProducersCache =
                new ConcurrentDictionary<ProducerConfig, IProducer<byte[]?, byte[]?>>(
                    new ConfigurationDictionaryComparer<string, string>());

        private readonly KafkaEventsHandler _kafkaEventsHandler;

        private readonly ISilverbackLogger _logger;

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        private IProducer<byte[]?, byte[]?>? _innerProducer;

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaProducer" /> class.
        /// </summary>
        /// <param name="broker">
        ///     The <see cref="IBroker" /> that instantiated this producer.
        /// </param>
        /// <param name="endpoint">
        ///     The endpoint to produce to.
        /// </param>
        /// <param name="behaviors">
        ///     The behaviors to be added to the pipeline.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackIntegrationLogger" />.
        /// </param>
        public KafkaProducer(
            KafkaBroker broker,
            KafkaProducerEndpoint endpoint,
            IReadOnlyList<IProducerBehavior>? behaviors,
            IServiceProvider serviceProvider,
            ISilverbackIntegrationLogger<KafkaProducer> logger)
            : base(broker, endpoint, behaviors, logger)
        {
            _logger = logger;

            _kafkaEventsHandler = serviceProvider.GetRequiredService<KafkaEventsHandler>();
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            DisposeInnerProducer();
        }

        /// <inheritdoc cref="Producer.ProduceCore" />
        protected override IOffset? ProduceCore(IOutboundEnvelope envelope) =>
            AsyncHelper.RunSynchronously(() => ProduceAsyncCore(envelope));

        /// <inheritdoc cref="Producer.ProduceAsyncCore" />
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        protected override async Task<IOffset?> ProduceAsyncCore(IOutboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            try
            {
                var kafkaMessage = new Message<byte[]?, byte[]?>
                {
                    Key = GetKafkaKeyAndRemoveHeader(envelope.Headers),
                    Value = envelope.RawMessage
                };

                if (envelope.Headers != null && envelope.Headers.Any())
                {
                    kafkaMessage.Headers = envelope.Headers.ToConfluentHeaders();
                }

                var deliveryResult = await GetInnerProducer().ProduceAsync(Endpoint.Name, kafkaMessage)
                    .ConfigureAwait(false);

                if (Endpoint.Configuration.ArePersistenceStatusReportsEnabled)
                {
                    CheckPersistenceStatus(deliveryResult);
                }

                var offset = new KafkaOffset(deliveryResult.TopicPartitionOffset);
                envelope.AdditionalLogData["offset"] = $"{offset.Partition}@{offset.Offset}";

                return offset;
            }
            catch (KafkaException ex)
            {
                // Disposing and re-creating the producer will maybe fix the issue
                if (Endpoint.Configuration.DisposeOnException)
                    DisposeInnerProducer();

                throw new ProduceException(
                    "Error occurred producing the message. See inner exception for details.",
                    ex);
            }
        }

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        private byte[]? GetKafkaKeyAndRemoveHeader(MessageHeaderCollection headers)
        {
            var kafkaKeyHeader = headers.FirstOrDefault(header => header.Name == KafkaMessageHeaders.KafkaMessageKey);

            if (kafkaKeyHeader == null)
                return null;

            headers.Remove(kafkaKeyHeader);

            if (kafkaKeyHeader.Value == null)
                return null;

            return Endpoint.Serializer is IKafkaMessageSerializer kafkaSerializer
                ? kafkaSerializer.SerializeKey(
                    kafkaKeyHeader.Value,
                    headers,
                    new MessageSerializationContext(Endpoint, Endpoint.Name))
                : Encoding.UTF8.GetBytes(kafkaKeyHeader.Value);
        }

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        private IProducer<byte[]?, byte[]?> GetInnerProducer() =>
            _innerProducer ??=
                ProducersCache.GetOrAdd(Endpoint.Configuration.ConfluentConfig, _ => CreateInnerProducer());

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        private IProducer<byte[]?, byte[]?> CreateInnerProducer()
        {
            _logger.LogDebug(KafkaEventIds.CreatingConfluentProducer, "Creating Confluent.Kafka.Producer...");

            var producerBuilder =
                new ProducerBuilder<byte[]?, byte[]?>(Endpoint.Configuration.ConfluentConfig);

            _kafkaEventsHandler.SetProducerEventsHandlers(producerBuilder);

            return producerBuilder.Build();
        }

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        private void CheckPersistenceStatus(DeliveryResult<byte[]?, byte[]?> deliveryReport)
        {
            switch (deliveryReport.Status)
            {
                case PersistenceStatus.PossiblyPersisted
                    when Endpoint.Configuration.ThrowIfNotAcknowledged:
                {
                    throw new ProduceException(
                        "The message was transmitted to broker, but no acknowledgement was received.");
                }

                case PersistenceStatus.PossiblyPersisted:
                {
                    _logger.LogWarning(
                        KafkaEventIds.ProduceNotAcknowledged,
                        "The message was transmitted to broker, but no acknowledgement was received.");
                    break;
                }

                case PersistenceStatus.NotPersisted:
                {
                    throw new ProduceException(
                        "The message was never transmitted to the broker, " +
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
    }
}
