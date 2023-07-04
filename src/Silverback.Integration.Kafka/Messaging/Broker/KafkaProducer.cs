// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="Producer{TBroker,TEndpoint}" />
    public class KafkaProducer : Producer<KafkaBroker, KafkaProducerEndpoint>, IDisposable
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
        /// <param name="producersCache">
        ///     The <see cref="IConfluentProducersCache" />.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="IOutboundLogger{TCategoryName}" />.
        /// </param>
        public KafkaProducer(
            KafkaBroker broker,
            KafkaProducerEndpoint endpoint,
            IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
            IConfluentProducersCache producersCache,
            IServiceProvider serviceProvider,
            IOutboundLogger<KafkaProducer> logger)
            : base(broker, endpoint, behaviorsProvider, serviceProvider, logger)
        {
            Check.NotNull(endpoint, nameof(endpoint));
            Check.NotNull(serviceProvider, nameof(serviceProvider));

            _confluentClientsCache = Check.NotNull(producersCache, nameof(producersCache));
            _logger = Check.NotNull(logger, nameof(logger));
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
        ///     resources.
        /// </summary>
        /// <param name="disposing">
        ///     A value indicating whether the method has been called by the <c>Dispose</c> method and not from the
        ///     finalizer.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            DisposeConfluentProducer();
        }

        /// <inheritdoc cref="Producer.ProduceCore(object,Stream,IReadOnlyCollection{MessageHeader},string)" />
        protected override IBrokerMessageIdentifier? ProduceCore(
            object? message,
            Stream? messageStream,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName) =>
            AsyncHelper.RunSynchronously(() => ProduceCoreAsync(message, messageStream, headers, actualEndpointName));

        /// <inheritdoc cref="Producer.ProduceCore(object,byte[],IReadOnlyCollection{MessageHeader},string)" />
        protected override IBrokerMessageIdentifier? ProduceCore(
            object? message,
            byte[]? messageBytes,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName) =>
            AsyncHelper.RunSynchronously(() => ProduceCoreAsync(message, messageBytes, headers, actualEndpointName));

        /// <inheritdoc cref="Producer.ProduceCore(object,Stream,IReadOnlyCollection{MessageHeader},string,Action{IBrokerMessageIdentifier},Action{Exception})" />
        protected override void ProduceCore(
            object? message,
            Stream? messageStream,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName,
            Action<IBrokerMessageIdentifier?> onSuccess,
            Action<Exception> onError) =>
            ProduceCore(message, messageStream.ReadAll(), headers, actualEndpointName, onSuccess, onError);

        /// <inheritdoc cref="Producer.ProduceCore(object,byte[],IReadOnlyCollection{MessageHeader},string,Action{IBrokerMessageIdentifier},Action{Exception})" />
        [SuppressMessage("", "CA1031", Justification = "Exception forwarded")]
        protected override void ProduceCore(
            object? message,
            byte[]? messageBytes,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName,
            Action<IBrokerMessageIdentifier?> onSuccess,
            Action<Exception> onError)
        {
            Check.NotNull(onSuccess, nameof(onSuccess));
            Check.NotNull(onError, nameof(onError));

            var kafkaMessage = new Message<byte[]?, byte[]?>
            {
                Key = GetKafkaKey(headers),
                Value = messageBytes
            };

            if (headers != null && headers.Count >= 1)
                kafkaMessage.Headers = headers.ToConfluentHeaders();

            var topicPartition = new TopicPartition(
                actualEndpointName,
                GetPartition(headers));

            GetConfluentProducer().Produce(
                topicPartition,
                kafkaMessage,
                deliveryReport =>
                {
                    try
                    {
                        if (deliveryReport.Error != null && deliveryReport.Error.IsError)
                        {
                            // Disposing and re-creating the producer will maybe fix the issue
                            if (Endpoint.Configuration.DisposeOnException)
                                DisposeConfluentProducer();

                            throw new ProduceException($"Error occurred producing the message. (error code {deliveryReport.Error.Code})");
                        }

                        if (Endpoint.Configuration.ArePersistenceStatusReportsEnabled)
                            CheckPersistenceStatus(deliveryReport);

                        onSuccess.Invoke(new KafkaOffset(deliveryReport.TopicPartitionOffsetError.TopicPartitionOffset));
                    }
                    catch (Exception ex)
                    {
                        onError.Invoke(ex);
                    }
                });
        }

        /// <inheritdoc cref="Producer.ProduceCoreAsync(object,Stream,IReadOnlyCollection{MessageHeader},string,CancellationToken)" />
        protected override async Task<IBrokerMessageIdentifier?> ProduceCoreAsync(
            object? message,
            Stream? messageStream,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName,
            CancellationToken cancellationToken = default) =>
            await ProduceCoreAsync(
                    message,
                    await messageStream.ReadAllAsync(cancellationToken).ConfigureAwait(false),
                    headers,
                    actualEndpointName,
                    cancellationToken)
                .ConfigureAwait(false);

        /// <inheritdoc cref="Producer.ProduceCoreAsync(object,byte[],IReadOnlyCollection{MessageHeader},string,CancellationToken)" />
        protected override async Task<IBrokerMessageIdentifier?> ProduceCoreAsync(
            object? message,
            byte[]? messageBytes,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var kafkaMessage = new Message<byte[]?, byte[]?>
                {
                    Key = GetKafkaKey(headers),
                    Value = messageBytes
                };

                if (headers is { Count: >= 1 })
                    kafkaMessage.Headers = headers.ToConfluentHeaders();

                var topicPartition = new TopicPartition(
                    actualEndpointName,
                    GetPartition(headers));

                var deliveryResult = await GetConfluentProducer().ProduceAsync(topicPartition, kafkaMessage, cancellationToken)
                    .ConfigureAwait(false);

                if (Endpoint.Configuration.ArePersistenceStatusReportsEnabled)
                    CheckPersistenceStatus(deliveryResult);

                return new KafkaOffset(deliveryResult.TopicPartitionOffset);
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

        /// <inheritdoc cref="Producer.ProduceCoreAsync(object,Stream,IReadOnlyCollection{MessageHeader},string,Action{IBrokerMessageIdentifier},Action{Exception},CancellationToken)" />
        protected override async Task ProduceCoreAsync(
            object? message,
            Stream? messageStream,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName,
            Action<IBrokerMessageIdentifier?> onSuccess,
            Action<Exception> onError,
            CancellationToken cancellationToken = default) =>
            await ProduceCoreAsync(
                    message,
                    await messageStream.ReadAllAsync(cancellationToken).ConfigureAwait(false),
                    headers,
                    actualEndpointName,
                    onSuccess,
                    onError,
                    cancellationToken)
                .ConfigureAwait(false);

        /// <inheritdoc cref="Producer.ProduceCoreAsync(object,byte[],IReadOnlyCollection{MessageHeader},string,Action{IBrokerMessageIdentifier},Action{Exception},CancellationToken)" />
        [SuppressMessage("", "CA1031", Justification = "Exception logged/forwarded")]
        protected override Task ProduceCoreAsync(
            object? message,
            byte[]? messageBytes,
            IReadOnlyCollection<MessageHeader>? headers,
            string actualEndpointName,
            Action<IBrokerMessageIdentifier?> onSuccess,
            Action<Exception> onError,
            CancellationToken cancellationToken = default)
        {
            ProduceCore(message, messageBytes, headers, actualEndpointName, onSuccess, onError);
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Returns the cached <see cref="IProducer{TKey,TValue}" /> or creates a new one.
        /// </summary>
        /// <returns>
        ///     The <see cref="IProducer{TKey,TValue}" />.
        /// </returns>
        protected IProducer<byte[]?, byte[]?> GetConfluentProducer() =>
            _confluentProducer ??= _confluentClientsCache.GetProducer(this);

        private static Partition GetPartition(IReadOnlyCollection<MessageHeader>? headers)
        {
            if (headers == null)
                return Partition.Any;

            var partitionHeader =
                headers.FirstOrDefault(header => header.Name == KafkaMessageHeaders.KafkaPartitionIndex);

            if (partitionHeader?.Value == null)
                return Partition.Any;

            return int.Parse(partitionHeader.Value, CultureInfo.InvariantCulture);
        }

        private byte[]? GetKafkaKey(IReadOnlyCollection<MessageHeader>? headers)
        {
            if (headers == null)
                return null;

            var kafkaKeyHeader =
                headers.FirstOrDefault(header => header.Name == KafkaMessageHeaders.KafkaMessageKey) ??
                headers.FirstOrDefault(header => header.Name == DefaultMessageHeaders.MessageId);

            if (kafkaKeyHeader?.Value == null)
                return null;

            return Endpoint.Serializer is IKafkaMessageSerializer kafkaSerializer
                ? kafkaSerializer.SerializeKey(
                    kafkaKeyHeader.Value,
                    headers,
                    new MessageSerializationContext(Endpoint, Endpoint.Name))
                : Encoding.UTF8.GetBytes(kafkaKeyHeader.Value);
        }

        private void CheckPersistenceStatus(DeliveryResult<byte[]?, byte[]?> deliveryReport)
        {
            switch (deliveryReport.Status)
            {
                case PersistenceStatus.PossiblyPersisted
                    when Endpoint.Configuration.ThrowIfNotAcknowledged:
                    throw new ProduceException("The message was transmitted to broker, but no acknowledgement was received.");

                case PersistenceStatus.PossiblyPersisted:
                    _logger.LogProduceNotAcknowledged(this);
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
            _confluentClientsCache.DisposeProducer(this);
            _confluentProducer = null;
        }
    }
}
