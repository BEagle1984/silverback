// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="Producer{TBroker,TEndpoint}" />
    public sealed class KafkaProducer : Producer<KafkaBroker, KafkaProducerEndpoint>, IDisposable
    {
        private readonly IConfluentProducersCache _confluentClientsCache;

        private readonly ISilverbackLogger _logger;

        private IProducer<byte[]?, byte[]?>? _confluentProducer;

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaProducer" /> class.
        /// </summary>
        /// <param name="broker">
        ///     The <see cref="IBroker" /> that instantiated this producer.
        /// </param>
        /// <param name="endpoint">
        ///     The endpoint to produce to.
        /// </param>
        /// <param name="behaviorsProvider">
        ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}" />.
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
            IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider,
            ISilverbackIntegrationLogger<KafkaProducer> logger)
            : base(broker, endpoint, behaviorsProvider, serviceProvider, logger)
        {
            Check.NotNull(endpoint, nameof(endpoint));
            Check.NotNull(serviceProvider, nameof(serviceProvider));

            _confluentClientsCache = serviceProvider.GetRequiredService<IConfluentProducersCache>();
            _logger = logger;
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            DisposeConfluentProducer();
        }

        /// <inheritdoc cref="Producer.ProduceCore" />
        protected override IBrokerMessageIdentifier? ProduceCore(IOutboundEnvelope envelope) =>
            AsyncHelper.RunSynchronously(() => ProduceCoreAsync(envelope));

        /// <inheritdoc cref="Producer.ProduceCoreAsync" />
        protected override async Task<IBrokerMessageIdentifier?> ProduceCoreAsync(IOutboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            try
            {
                var kafkaMessage = new Message<byte[]?, byte[]?>
                {
                    Key = GetKafkaKeyAndRemoveHeader(envelope.Headers),
                    Value = await envelope.RawMessage.ReadAllAsync().ConfigureAwait(false)
                };

                if (envelope.Headers.Count >= 1)
                {
                    kafkaMessage.Headers = envelope.Headers.ToConfluentHeaders();
                }

                var deliveryResult = await GetConfluentProducer().ProduceAsync(Endpoint.Name, kafkaMessage)
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
                    DisposeConfluentProducer();

                throw new ProduceException(
                    "Error occurred producing the message. See inner exception for details.",
                    ex);
            }
        }

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

        private IProducer<byte[]?, byte[]?> GetConfluentProducer() =>
            _confluentProducer ??= _confluentClientsCache.GetProducer(Endpoint.Configuration, this);

        private void CheckPersistenceStatus(DeliveryResult<byte[]?, byte[]?> deliveryReport)
        {
            switch (deliveryReport.Status)
            {
                case PersistenceStatus.PossiblyPersisted
                    when Endpoint.Configuration.ThrowIfNotAcknowledged:
                    throw new ProduceException(
                        "The message was transmitted to broker, but no acknowledgement was received.");

                case PersistenceStatus.PossiblyPersisted:
                    _logger.LogWarning(
                        KafkaEventIds.ProduceNotAcknowledged,
                        "The message was transmitted to broker, but no acknowledgement was received.");
                    break;

                case PersistenceStatus.NotPersisted:
                    throw new ProduceException(
                        "The message was never transmitted to the broker, " +
                        "or failed with an error indicating it was not written " +
                        "to the log.'");
            }
        }

        private void DisposeConfluentProducer()
        {
            _confluentClientsCache.DisposeProducer(Endpoint.Configuration);
            _confluentProducer = null;
        }
    }
}
