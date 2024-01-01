// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Transactions;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

/// <inheritdoc cref="Producer{TEndpoint}" />
// TODO: Overloads (or extensions) accepting an additional KafkaTransaction parameter
public sealed class KafkaTransactionalProducer : IProducer
{
    private readonly IKafkaTransactionalProducerCollection _transactionalProducers;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaTransactionalProducer" /> class.
    /// </summary>
    /// <param name="name">
    ///     The producer identifier.
    /// </param>
    /// <param name="configuration">
    ///     The configuration containing only the actual endpoint.
    /// </param>
    /// <param name="transactionalProducers">
    ///     The <see cref="IKafkaTransactionalProducerCollection" />.
    /// </param>
    public KafkaTransactionalProducer(
        string name,
        KafkaProducerConfiguration configuration,
        IKafkaTransactionalProducerCollection transactionalProducers)
    {
        Name = Check.NotNullOrEmpty(name, nameof(name));
        Configuration = Check.NotNull(configuration, nameof(configuration));
        _transactionalProducers = Check.NotNull(transactionalProducers, nameof(transactionalProducers));

        EndpointConfiguration = configuration.Endpoints.Single();
    }

    /// <inheritdoc cref="IProducer.Name" />
    public string Name { get; }

    /// <inheritdoc cref="IProducer.DisplayName" />
    public string DisplayName => Name;

    /// <summary>
    ///     Gets the producer configuration.
    /// </summary>
    public KafkaProducerConfiguration Configuration { get; }

    /// <inheritdoc cref="IProducer.EndpointConfiguration" />
    public ProducerEndpointConfiguration EndpointConfiguration { get; }

    /// <inheritdoc cref="IProducer.Produce(object?, IReadOnlyCollection{MessageHeader}?)" />
    public IBrokerMessageIdentifier Produce(object? message, IReadOnlyCollection<MessageHeader>? headers = null) =>
        throw new NotSupportedException("Use the overload accepting an IOutboundEnvelope instead.");

    /// <inheritdoc cref="IProducer.Produce(IOutboundEnvelope)" />
    public IBrokerMessageIdentifier? Produce(IOutboundEnvelope envelope) =>
        GetProducerForTransaction(Check.NotNull(envelope, nameof(envelope))).Produce(envelope);

    /// <inheritdoc cref="IProducer.Produce(object?, IReadOnlyCollection{MessageHeader}?, Action{IBrokerMessageIdentifier?}, Action{Exception})" />
    public void Produce(object? message, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) =>
        throw new NotSupportedException("Use the overload accepting an IOutboundEnvelope instead.");

    /// <inheritdoc cref="IProducer.Produce(IOutboundEnvelope, Action{IBrokerMessageIdentifier?}, Action{Exception})" />
    public void Produce(IOutboundEnvelope envelope, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) =>
        GetProducerForTransaction(Check.NotNull(envelope, nameof(envelope))).Produce(envelope, onSuccess, onError);

    /// <inheritdoc cref="IProducer.RawProduce(byte[], IReadOnlyCollection{MessageHeader}?)" />
    public IBrokerMessageIdentifier RawProduce(byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers = null) =>
        throw new NotSupportedException("Use the overload accepting an IOutboundEnvelope instead.");

    /// <inheritdoc cref="IProducer.RawProduce(Stream?, IReadOnlyCollection{MessageHeader}?)" />
    public IBrokerMessageIdentifier RawProduce(Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers = null) =>
        throw new NotSupportedException("Use the overload accepting an IOutboundEnvelope instead.");

    /// <inheritdoc cref="IProducer.RawProduce(ProducerEndpoint, byte[], IReadOnlyCollection{MessageHeader}?)" />
    public IBrokerMessageIdentifier RawProduce(ProducerEndpoint endpoint, byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers = null) =>
        throw new NotSupportedException("Use the overload accepting an IOutboundEnvelope instead.");

    /// <inheritdoc cref="IProducer.RawProduce(ProducerEndpoint, Stream?, IReadOnlyCollection{MessageHeader}?)" />
    public IBrokerMessageIdentifier RawProduce(ProducerEndpoint endpoint, Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers = null) =>
        throw new NotSupportedException("Use the overload accepting an IOutboundEnvelope instead.");

    /// <inheritdoc cref="IProducer.RawProduce(byte[], IReadOnlyCollection{MessageHeader}?, Action{IBrokerMessageIdentifier?}, Action{Exception})" />
    public void RawProduce(byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) =>
        throw new NotSupportedException("Use the overload accepting an IOutboundEnvelope instead.");

    /// <inheritdoc cref="IProducer.RawProduce(Stream?, IReadOnlyCollection{MessageHeader}?, Action{IBrokerMessageIdentifier?}, Action{Exception})" />
    public void RawProduce(Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) =>
        throw new NotSupportedException("Use the overload accepting an IOutboundEnvelope instead.");

    /// <inheritdoc cref="IProducer.RawProduce(ProducerEndpoint, byte[], IReadOnlyCollection{MessageHeader}?, Action{IBrokerMessageIdentifier?}, Action{Exception})" />
    public void RawProduce(ProducerEndpoint endpoint, byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) =>
        throw new NotSupportedException("Use the overload accepting an IOutboundEnvelope instead.");

    /// <inheritdoc cref="IProducer.RawProduce(ProducerEndpoint, Stream?, IReadOnlyCollection{MessageHeader}?, Action{IBrokerMessageIdentifier?}, Action{Exception})" />
    public void RawProduce(ProducerEndpoint endpoint, Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) =>
        throw new NotSupportedException("Use the overload accepting an IOutboundEnvelope instead.");

    /// <inheritdoc cref="IProducer.ProduceAsync(object?, IReadOnlyCollection{MessageHeader}?)" />
    public ValueTask<IBrokerMessageIdentifier?> ProduceAsync(object? message, IReadOnlyCollection<MessageHeader>? headers = null) =>
        throw new NotSupportedException("Use the overload accepting an IOutboundEnvelope instead.");

    /// <inheritdoc cref="IProducer.ProduceAsync(IOutboundEnvelope)" />
    public async ValueTask<IBrokerMessageIdentifier?> ProduceAsync(IOutboundEnvelope envelope)
    {
        KafkaProducer producer = await GetProducerForTransactionAsync(Check.NotNull(envelope, nameof(envelope))).ConfigureAwait(false);
        return await producer.ProduceAsync(envelope).ConfigureAwait(false);
    }

    /// <inheritdoc cref="IProducer.ProduceAsync(object?, IReadOnlyCollection{MessageHeader}?, Action{IBrokerMessageIdentifier?}, Action{Exception})" />
    public ValueTask ProduceAsync(object? message, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) =>
        throw new NotSupportedException("Use the overload accepting an IOutboundEnvelope instead.");

    /// <inheritdoc cref="IProducer.ProduceAsync(IOutboundEnvelope, Action{IBrokerMessageIdentifier?}, Action{Exception})" />
    public async ValueTask ProduceAsync(IOutboundEnvelope envelope, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError)
    {
        KafkaProducer producer = await GetProducerForTransactionAsync(Check.NotNull(envelope, nameof(envelope))).ConfigureAwait(false);
        await producer.ProduceAsync(envelope, onSuccess, onError).ConfigureAwait(false);
    }

    /// <inheritdoc cref="IProducer.RawProduceAsync(byte[], IReadOnlyCollection{MessageHeader}?)" />
    public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(byte[]? message, IReadOnlyCollection<MessageHeader>? headers = null) =>
        throw new NotSupportedException("Use the overload accepting an IOutboundEnvelope instead.");

    /// <inheritdoc cref="IProducer.RawProduceAsync(Stream?, IReadOnlyCollection{MessageHeader}?)" />
    public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(Stream? message, IReadOnlyCollection<MessageHeader>? headers = null) =>
        throw new NotSupportedException("Use the overload accepting an IOutboundEnvelope instead.");

    /// <inheritdoc cref="IProducer.RawProduceAsync(ProducerEndpoint, byte[], IReadOnlyCollection{MessageHeader}?)" />
    public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(ProducerEndpoint endpoint, byte[]? message, IReadOnlyCollection<MessageHeader>? headers = null) =>
        throw new NotSupportedException("Use the overload accepting an IOutboundEnvelope instead.");

    /// <inheritdoc cref="IProducer.RawProduceAsync(ProducerEndpoint, Stream?, IReadOnlyCollection{MessageHeader}?)" />
    public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(ProducerEndpoint endpoint, Stream? message, IReadOnlyCollection<MessageHeader>? headers = null) =>
        throw new NotSupportedException("Use the overload accepting an IOutboundEnvelope instead.");

    /// <inheritdoc cref="IProducer.RawProduceAsync(byte[], IReadOnlyCollection{MessageHeader}?, Action{IBrokerMessageIdentifier?}, Action{Exception})" />
    public ValueTask RawProduceAsync(byte[]? message, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) =>
        throw new NotSupportedException("Use the overload accepting an IOutboundEnvelope instead.");

    /// <inheritdoc cref="IProducer.RawProduceAsync(Stream?, IReadOnlyCollection{MessageHeader}?, Action{IBrokerMessageIdentifier?}, Action{Exception})" />
    public ValueTask RawProduceAsync(Stream? message, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) =>
        throw new NotSupportedException("Use the overload accepting an IOutboundEnvelope instead.");

    /// <inheritdoc cref="IProducer.RawProduceAsync(ProducerEndpoint, byte[], IReadOnlyCollection{MessageHeader}?, Action{IBrokerMessageIdentifier?}, Action{Exception})" />
    public ValueTask RawProduceAsync(ProducerEndpoint endpoint, byte[]? message, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) =>
        throw new NotSupportedException("Use the overload accepting an IOutboundEnvelope instead.");

    /// <inheritdoc cref="IProducer.RawProduceAsync(ProducerEndpoint, Stream?, IReadOnlyCollection{MessageHeader}?, Action{IBrokerMessageIdentifier?}, Action{Exception})" />
    public ValueTask RawProduceAsync(ProducerEndpoint endpoint, Stream? message, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) =>
        throw new NotSupportedException("Use the overload accepting an IOutboundEnvelope instead.");

    private KafkaProducer GetProducerForTransaction(IOutboundEnvelope envelope) =>
        AsyncHelper.RunSynchronously(() => GetProducerForTransactionAsync(envelope));

    private async ValueTask<KafkaProducer> GetProducerForTransactionAsync(IOutboundEnvelope envelope)
    {
        KafkaTransaction transaction = envelope.Context?.GetKafkaTransaction()
                                       ?? throw new InvalidOperationException("The transaction is not available."); // TODO: Improve message and exception type

        KafkaProducer producer = await _transactionalProducers.GetOrCreateAsync(Name, Configuration, envelope, transaction).ConfigureAwait(false);

        transaction.BindConfluentProducer(producer.Client);
        transaction.EnsureBegin();

        return producer;
    }
}
