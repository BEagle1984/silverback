// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

/// <inheritdoc cref="Producer{TBroker,TEndpoint,TActualEndpoint}" />
public sealed class KafkaProducer : Producer<KafkaBroker, KafkaProducerConfiguration, KafkaProducerEndpoint>, IDisposable
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
    /// <param name="configuration">
    ///     The <see cref="KafkaProducerConfiguration" />.
    /// </param>
    /// <param name="behaviorsProvider">
    ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}" />.
    /// </param>
    /// <param name="envelopeFactory">
    ///     The <see cref="IOutboundEnvelopeFactory" />.
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
        KafkaProducerConfiguration configuration,
        IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
        IOutboundEnvelopeFactory envelopeFactory,
        IConfluentProducersCache producersCache,
        IServiceProvider serviceProvider,
        IOutboundLogger<KafkaProducer> logger)
        : base(broker, configuration, behaviorsProvider, envelopeFactory, serviceProvider, logger)
    {
        Check.NotNull(configuration, nameof(configuration));
        Check.NotNull(serviceProvider, nameof(serviceProvider));

        _confluentClientsCache = Check.NotNull(producersCache, nameof(producersCache));
        _logger = Check.NotNull(logger, nameof(logger));
    }

    /// <inheritdoc cref="IDisposable.Dispose" />
    public void Dispose()
    {
        DisposeConfluentProducer();
    }

    /// <inheritdoc cref="Producer{TBroker,TEndpoint,TActualEndpoint}.ProduceCore(object,Stream,IReadOnlyCollection{MessageHeader},TActualEndpoint)" />
    protected override IBrokerMessageIdentifier? ProduceCore(
        object? message,
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers,
        KafkaProducerEndpoint endpoint) =>
        AsyncHelper.RunSynchronously(() => ProduceCoreAsync(message, messageStream, headers, endpoint));

    /// <inheritdoc cref="Producer{TBroker,TEndpoint,TActualEndpoint}.ProduceCore(object,byte[],IReadOnlyCollection{MessageHeader},TActualEndpoint)" />
    protected override IBrokerMessageIdentifier? ProduceCore(
        object? message,
        byte[]? messageBytes,
        IReadOnlyCollection<MessageHeader>? headers,
        KafkaProducerEndpoint endpoint) =>
        AsyncHelper.RunSynchronously(() => ProduceCoreAsync(message, messageBytes, headers, endpoint));

    /// <inheritdoc cref="Producer{TBroker,TEndpoint,TActualEndpoint}.ProduceCore(object,Stream,IReadOnlyCollection{MessageHeader},TActualEndpoint,Action{IBrokerMessageIdentifier},Action{Exception})" />
    protected override void ProduceCore(
        object? message,
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers,
        KafkaProducerEndpoint endpoint,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError) =>
        ProduceCore(message, messageStream.ReadAll(), headers, endpoint, onSuccess, onError);

    /// <inheritdoc cref="Producer{TBroker,TEndpoint,TActualEndpoint}.ProduceCore(object,byte[],IReadOnlyCollection{MessageHeader},TActualEndpoint,Action{IBrokerMessageIdentifier},Action{Exception})" />
    [SuppressMessage("", "CA1031", Justification = "Exception forwarded")]
    protected override void ProduceCore(
        object? message,
        byte[]? messageBytes,
        IReadOnlyCollection<MessageHeader>? headers,
        KafkaProducerEndpoint endpoint,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError)
    {
        Check.NotNull(endpoint, nameof(endpoint));
        Check.NotNull(onSuccess, nameof(onSuccess));
        Check.NotNull(onError, nameof(onError));

        Message<byte[]?, byte[]?> kafkaMessage = new()
        {
            Key = GetKafkaKey(headers, endpoint),
            Value = messageBytes
        };

        if (headers != null && headers.Count >= 1)
            kafkaMessage.Headers = headers.ToConfluentHeaders();

        GetConfluentProducer().Produce(
            endpoint.TopicPartition,
            kafkaMessage,
            deliveryReport =>
            {
                try
                {
                    if (deliveryReport.Error != null && deliveryReport.Error.IsError)
                    {
                        // Disposing and re-creating the producer will maybe fix the issue
                        if (Configuration.Client.DisposeOnException)
                            DisposeConfluentProducer();

                        throw new ProduceException($"Error occurred producing the message. (error code {deliveryReport.Error.Code})");
                    }

                    if (Configuration.Client.ArePersistenceStatusReportsEnabled)
                        CheckPersistenceStatus(deliveryReport);

                    onSuccess.Invoke(new KafkaOffset(deliveryReport.TopicPartitionOffsetError.TopicPartitionOffset));
                }
                catch (Exception ex)
                {
                    onError.Invoke(ex);
                }
            });
    }

    /// <inheritdoc cref="Producer{TBroker,TEndpoint,TActualEndpoint}.ProduceCoreAsync(object,Stream,IReadOnlyCollection{MessageHeader},TActualEndpoint)" />
    protected override async Task<IBrokerMessageIdentifier?> ProduceCoreAsync(
        object? message,
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers,
        KafkaProducerEndpoint endpoint) =>
        await ProduceCoreAsync(message, await messageStream.ReadAllAsync().ConfigureAwait(false), headers, endpoint).ConfigureAwait(false);

    /// <inheritdoc cref="Producer{TBroker,TEndpoint,TActualEndpoint}.ProduceCoreAsync(object,byte[],IReadOnlyCollection{MessageHeader},TActualEndpoint)" />
    protected override async Task<IBrokerMessageIdentifier?> ProduceCoreAsync(
        object? message,
        byte[]? messageBytes,
        IReadOnlyCollection<MessageHeader>? headers,
        KafkaProducerEndpoint endpoint)
    {
        Check.NotNull(endpoint, nameof(endpoint));

        try
        {
            Message<byte[]?, byte[]?> kafkaMessage = new()
            {
                Key = GetKafkaKey(headers, endpoint),
                Value = messageBytes
            };

            if (headers is { Count: >= 1 })
                kafkaMessage.Headers = headers.ToConfluentHeaders();

            DeliveryResult<byte[]?, byte[]?>? deliveryResult = await GetConfluentProducer()
                .ProduceAsync(endpoint.TopicPartition, kafkaMessage)
                .ConfigureAwait(false);

            if (Configuration.Client.ArePersistenceStatusReportsEnabled)
                CheckPersistenceStatus(deliveryResult);

            return new KafkaOffset(deliveryResult.TopicPartitionOffset);
        }
        catch (KafkaException ex)
        {
            // Disposing and re-creating the producer will maybe fix the issue
            if (Configuration.Client.DisposeOnException)
                DisposeConfluentProducer();

            throw new ProduceException(
                "Error occurred producing the message. See inner exception for details.",
                ex);
        }
    }

    /// <inheritdoc cref="Producer{TBroker,TEndpoint,TActualEndpoint}.ProduceCoreAsync(object,Stream,IReadOnlyCollection{MessageHeader},TActualEndpoint,Action{IBrokerMessageIdentifier},Action{Exception})" />
    protected override async Task ProduceCoreAsync(
        object? message,
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers,
        KafkaProducerEndpoint endpoint,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError) =>
        await ProduceCoreAsync(
                message,
                await messageStream.ReadAllAsync().ConfigureAwait(false),
                headers,
                endpoint,
                onSuccess,
                onError)
            .ConfigureAwait(false);

    /// <inheritdoc cref="Producer{TBroker,TEndpoint,TActualEndpoint}.ProduceCoreAsync(object,byte[],IReadOnlyCollection{MessageHeader},TActualEndpoint,Action{IBrokerMessageIdentifier},Action{Exception})" />
    [SuppressMessage("", "CA1031", Justification = "Exception logged/forwarded")]
    protected override Task ProduceCoreAsync(
        object? message,
        byte[]? messageBytes,
        IReadOnlyCollection<MessageHeader>? headers,
        KafkaProducerEndpoint endpoint,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError)
    {
        ProduceCore(message, messageBytes, headers, endpoint, onSuccess, onError);
        return Task.CompletedTask;
    }

    private byte[]? GetKafkaKey(IReadOnlyCollection<MessageHeader>? headers, KafkaProducerEndpoint endpoint)
    {
        if (headers == null)
            return null;

        if (!headers.TryGetValue(KafkaMessageHeaders.KafkaMessageKey, out string? kafkaKeyValueFromHeader))
            kafkaKeyValueFromHeader = headers.GetValue(DefaultMessageHeaders.MessageId);

        if (kafkaKeyValueFromHeader == null)
            return null;

        return Configuration.Serializer is IKafkaMessageSerializer kafkaSerializer
            ? kafkaSerializer.SerializeKey(
                kafkaKeyValueFromHeader,
                headers,
                endpoint)
            : Encoding.UTF8.GetBytes(kafkaKeyValueFromHeader);
    }

    private IProducer<byte[]?, byte[]?> GetConfluentProducer() =>
        _confluentProducer ??= _confluentClientsCache.GetProducer(Configuration.Client, this);

    private void CheckPersistenceStatus(DeliveryResult<byte[]?, byte[]?> deliveryReport)
    {
        switch (deliveryReport.Status)
        {
            case PersistenceStatus.PossiblyPersisted
                when Configuration.Client.ThrowIfNotAcknowledged:
                throw new ProduceException("The message was transmitted to broker, but no acknowledgement was received.");

            case PersistenceStatus.PossiblyPersisted:
                _logger.LogProduceNotAcknowledged(this);
                break;

            case PersistenceStatus.NotPersisted:
                throw new ProduceException(
                    "The message was never transmitted to the broker, or failed with an error indicating it " +
                    "was not written to the log.'");
        }
    }

    private void DisposeConfluentProducer()
    {
        _confluentClientsCache.DisposeProducer(Configuration.Client);
        _confluentProducer = null;
    }
}
