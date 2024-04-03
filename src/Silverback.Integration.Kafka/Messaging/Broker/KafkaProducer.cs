// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.Routing;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

/// <inheritdoc cref="Producer" />
public sealed class KafkaProducer : Producer
{
    private readonly ISilverbackLogger _logger;

    private readonly IKafkaMessageSerializer _serializer;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaProducer" /> class.
    /// </summary>
    /// <param name="name">
    ///     The producer identifier.
    /// </param>
    /// <param name="client">
    ///     The <see cref="IConfluentProducerWrapper" />.
    /// </param>
    /// <param name="configuration">
    ///     The configuration containing only the actual endpoint.
    /// </param>
    /// <param name="behaviorsProvider">
    ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}" />.
    /// </param>
    /// <param name="envelopeFactory">
    ///     The <see cref="IOutboundEnvelopeFactory" />.
    /// </param>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="IProducerLogger{TCategoryName}" />.
    /// </param>
    public KafkaProducer(
        string name,
        IConfluentProducerWrapper client,
        KafkaProducerConfiguration configuration,
        IBrokerBehaviorsProvider<IProducerBehavior> behaviorsProvider,
        IOutboundEnvelopeFactory envelopeFactory,
        IServiceProvider serviceProvider,
        IProducerLogger<KafkaProducer> logger)
        : base(
            name,
            client,
            Check.NotNull(configuration, nameof(configuration)).Endpoints.Single(),
            behaviorsProvider,
            envelopeFactory,
            serviceProvider,
            logger)
    {
        Client = Check.NotNull(client, nameof(client));
        Configuration = Check.NotNull(configuration, nameof(configuration));
        _logger = Check.NotNull(logger, nameof(logger));

        EndpointConfiguration = Configuration.Endpoints.Single();
        _serializer = EndpointConfiguration.Serializer as IKafkaMessageSerializer ??
                      new DefaultKafkaMessageSerializer(EndpointConfiguration.Serializer);
    }

    /// <inheritdoc cref="Producer.Client" />
    public new IConfluentProducerWrapper Client { get; }

    /// <summary>
    ///     Gets the producer configuration.
    /// </summary>
    public KafkaProducerConfiguration Configuration { get; }

    /// <inheritdoc cref="Producer.EndpointConfiguration" />
    public new KafkaProducerEndpointConfiguration EndpointConfiguration { get; }

    /// <inheritdoc cref="Producer.ProduceCore(IOutboundEnvelope)" />
    protected override IBrokerMessageIdentifier? ProduceCore(IOutboundEnvelope envelope) =>
        ProduceCoreAsync(envelope).SafeWait(); // TODO: No better option?

    /// <inheritdoc cref="Producer.ProduceCore(IOutboundEnvelope,Action{IBrokerMessageIdentifier},Action{Exception})" />
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception forwarded")]
    protected override void ProduceCore(
        IOutboundEnvelope envelope,
        Action<IBrokerMessageIdentifier?> onSuccess,
        Action<Exception> onError)
    {
        Check.NotNull(envelope, nameof(envelope));
        Check.NotNull(onSuccess, nameof(onSuccess));
        Check.NotNull(onError, nameof(onError));

        KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)envelope.Endpoint;

        Message<byte[]?, byte[]?> kafkaMessage = new()
        {
            Key = GetKafkaKey(envelope, endpoint),
            Value = envelope.RawMessage.ReadAll()
        };

        if (envelope.Headers.Count >= 1)
            kafkaMessage.Headers = envelope.Headers.ToConfluentHeaders();

        Client.Produce(
            endpoint.TopicPartition,
            kafkaMessage,
            deliveryReport =>
            {
                try
                {
                    if (deliveryReport.Error != null && deliveryReport.Error.IsError)
                        throw new ProduceException($"Error occurred producing the message. (error code {deliveryReport.Error.Code})");

                    if (Configuration.ArePersistenceStatusReportsEnabled)
                        CheckPersistenceStatus(deliveryReport);

                    onSuccess.Invoke(new KafkaOffset(deliveryReport.TopicPartitionOffsetError.TopicPartitionOffset));
                }
                catch (Exception ex)
                {
                    onError.Invoke(ex);
                }
            });
    }

    /// <inheritdoc cref="Producer.ProduceCoreAsync(IOutboundEnvelope)" />
    protected override async ValueTask<IBrokerMessageIdentifier?> ProduceCoreAsync(IOutboundEnvelope envelope)
    {
        Check.NotNull(envelope, nameof(envelope));

        try
        {
            KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)envelope.Endpoint;

            Message<byte[]?, byte[]?> kafkaMessage = new()
            {
                Key = GetKafkaKey(envelope, endpoint),
                Value = await envelope.RawMessage.ReadAllAsync().ConfigureAwait(false)
            };

            if (envelope.Headers.Count >= 1)
                kafkaMessage.Headers = envelope.Headers.ToConfluentHeaders();

            DeliveryResult<byte[]?, byte[]?> deliveryResult = await Client.ProduceAsync(
                    endpoint.TopicPartition,
                    kafkaMessage)
                .ConfigureAwait(false);

            if (Configuration.ArePersistenceStatusReportsEnabled)
                CheckPersistenceStatus(deliveryResult);

            return new KafkaOffset(deliveryResult.TopicPartitionOffset);
        }
        catch (KafkaException ex)
        {
            throw new ProduceException("Error occurred producing the message. See inner exception for details.", ex);
        }
    }

    private byte[]? GetKafkaKey(IOutboundEnvelope envelope, KafkaProducerEndpoint endpoint)
    {
        if (!envelope.Headers.TryGetValue(DefaultMessageHeaders.MessageId, out string? kafkaKey) || kafkaKey == null)
            return null;

        return _serializer.SerializeKey(kafkaKey, envelope.Headers, endpoint);
    }

    private void CheckPersistenceStatus(DeliveryResult<byte[]?, byte[]?>? deliveryReport)
    {
        if (deliveryReport == null)
            throw new ProduceException("The delivery report is null.");

        switch (deliveryReport.Status)
        {
            case PersistenceStatus.PossiblyPersisted
                when Configuration.ThrowIfNotAcknowledged:
                throw new ProduceException("The message was transmitted to broker, but no acknowledgement was received.");

            case PersistenceStatus.PossiblyPersisted:
                _logger.LogProduceNotAcknowledged(this, deliveryReport.TopicPartition);
                break;

            case PersistenceStatus.NotPersisted:
                throw new ProduceException(
                    "The message was never transmitted to the broker, or failed with an error indicating it " +
                    "was not written to the log.'");
        }
    }
}
