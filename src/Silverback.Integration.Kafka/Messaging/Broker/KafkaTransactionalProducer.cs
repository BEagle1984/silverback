// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Transactions;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

/// <inheritdoc cref="Producer" />
public sealed class KafkaTransactionalProducer : IProducer
{
    private const string UseEnvelopeOverloadMessage = "Use the overload accepting an IOutboundEnvelope instead.";

    private readonly IKafkaTransactionalProducerCollection _transactionalProducers;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaTransactionalProducer" /> class.
    /// </summary>
    /// <param name="name">
    ///     The producer identifier. Must not be empty.
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
        Name = Check.NotNullOrEmpty(name);
        Configuration = Check.NotNull(configuration);
        _transactionalProducers = Check.NotNull(transactionalProducers);

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

    /// <inheritdoc cref="IProducer.Produce(object?, IReadOnlyCollection{MessageHeader}?)" path="/*[not(self::exception)]" />
    /// <exception cref="NotSupportedException">
    ///     Always thrown, because only the overloads accepting an <see cref="IOutboundEnvelope" /> are supported.
    /// </exception>
    public IBrokerMessageIdentifier Produce(object? message, IReadOnlyCollection<MessageHeader>? headers = null) =>
        throw new NotSupportedException(UseEnvelopeOverloadMessage);

    /// <inheritdoc cref="IProducer.Produce(IOutboundEnvelope)" />
    /// <exception cref="MissingKafkaTransactionException">
    ///     The <paramref name="envelope" /> has no context.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     No Kafka transaction was initialized in the context of the <paramref name="envelope" />, or the transaction is already bound to
    ///     another producer.
    /// </exception>
    public IBrokerMessageIdentifier? Produce(IOutboundEnvelope envelope) =>
        GetProducerForTransaction(Check.NotNull(envelope)).Produce(envelope);

    /// <inheritdoc cref="IProducer.Produce(object?, IReadOnlyCollection{MessageHeader}?, Action{IBrokerMessageIdentifier?}, Action{Exception})" path="/*[not(self::exception)]" />
    /// <exception cref="NotSupportedException">
    ///     Always thrown, because only the overloads accepting an <see cref="IOutboundEnvelope" /> are supported.
    /// </exception>
    public void Produce(object? message, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) =>
        throw new NotSupportedException(UseEnvelopeOverloadMessage);

    /// <inheritdoc cref="IProducer.Produce{TState}(object?, IReadOnlyCollection{MessageHeader}?, Action{IBrokerMessageIdentifier?, TState}, Action{Exception, TState}, TState)" path="/*[not(self::exception)]" />
    /// <exception cref="NotSupportedException">
    ///     Always thrown, because only the overloads accepting an <see cref="IOutboundEnvelope" /> are supported.
    /// </exception>
    public void Produce<TState>(object? message, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?, TState> onSuccess, Action<Exception, TState> onError, TState state) =>
        throw new NotSupportedException(UseEnvelopeOverloadMessage);

    /// <inheritdoc cref="IProducer.Produce(IOutboundEnvelope, Action{IBrokerMessageIdentifier?}, Action{Exception})" />
    /// <exception cref="MissingKafkaTransactionException">
    ///     The <paramref name="envelope" /> has no context.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     No Kafka transaction was initialized in the context of the <paramref name="envelope" />, or the transaction is already bound to
    ///     another producer.
    /// </exception>
    public void Produce(IOutboundEnvelope envelope, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) =>
        GetProducerForTransaction(Check.NotNull(envelope)).Produce(envelope, onSuccess, onError);

    /// <inheritdoc cref="IProducer.Produce{TState}(IOutboundEnvelope, Action{IBrokerMessageIdentifier?, TState}, Action{Exception, TState}, TState)" />
    /// <exception cref="MissingKafkaTransactionException">
    ///     The <paramref name="envelope" /> has no context.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     No Kafka transaction was initialized in the context of the <paramref name="envelope" />, or the transaction is already bound to
    ///     another producer.
    /// </exception>
    public void Produce<TState>(IOutboundEnvelope envelope, Action<IBrokerMessageIdentifier?, TState> onSuccess, Action<Exception, TState> onError, TState state) =>
        GetProducerForTransaction(Check.NotNull(envelope)).Produce(envelope, onSuccess, onError, state);

    /// <inheritdoc cref="IProducer.RawProduce(byte[], IReadOnlyCollection{MessageHeader}?)" path="/*[not(self::exception)]" />
    /// <exception cref="NotSupportedException">
    ///     Always thrown, because only the overloads accepting an <see cref="IOutboundEnvelope" /> are supported.
    /// </exception>
    public IBrokerMessageIdentifier RawProduce(byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers = null) =>
        throw new NotSupportedException(UseEnvelopeOverloadMessage);

    /// <inheritdoc cref="IProducer.RawProduce(Stream?, IReadOnlyCollection{MessageHeader}?)" path="/*[not(self::exception)]" />
    /// <exception cref="NotSupportedException">
    ///     Always thrown, because only the overloads accepting an <see cref="IOutboundEnvelope" /> are supported.
    /// </exception>
    public IBrokerMessageIdentifier RawProduce(Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers = null) =>
        throw new NotSupportedException(UseEnvelopeOverloadMessage);

    /// <inheritdoc cref="IProducer.RawProduce(byte[], IReadOnlyCollection{MessageHeader}?, Action{IBrokerMessageIdentifier?}, Action{Exception})" />
    /// <exception cref="NotSupportedException">
    ///     Always thrown, because only the overloads accepting an <see cref="IOutboundEnvelope" /> are supported.
    /// </exception>
    public void RawProduce(byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) =>
        throw new NotSupportedException(UseEnvelopeOverloadMessage);

    /// <inheritdoc cref="IProducer.RawProduce{TState}(byte[], IReadOnlyCollection{MessageHeader}?, Action{IBrokerMessageIdentifier?, TState}, Action{Exception, TState}, TState)" />
    /// <exception cref="NotSupportedException">
    ///     Always thrown, because only the overloads accepting an <see cref="IOutboundEnvelope" /> are supported.
    /// </exception>
    public void RawProduce<TState>(byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?, TState> onSuccess, Action<Exception, TState> onError, TState state) =>
        throw new NotSupportedException(UseEnvelopeOverloadMessage);

    /// <inheritdoc cref="IProducer.RawProduce(Stream?, IReadOnlyCollection{MessageHeader}?, Action{IBrokerMessageIdentifier?}, Action{Exception})" />
    /// <exception cref="NotSupportedException">
    ///     Always thrown, because only the overloads accepting an <see cref="IOutboundEnvelope" /> are supported.
    /// </exception>
    public void RawProduce(Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) =>
        throw new NotSupportedException(UseEnvelopeOverloadMessage);

    /// <inheritdoc cref="IProducer.RawProduce{TState}(Stream?, IReadOnlyCollection{MessageHeader}?, Action{IBrokerMessageIdentifier?, TState}, Action{Exception, TState}, TState)" />
    /// <exception cref="NotSupportedException">
    ///     Always thrown, because only the overloads accepting an <see cref="IOutboundEnvelope" /> are supported.
    /// </exception>
    public void RawProduce<TState>(Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?, TState> onSuccess, Action<Exception, TState> onError, TState state) =>
        throw new NotSupportedException(UseEnvelopeOverloadMessage);

    /// <inheritdoc cref="IProducer.ProduceAsync(object?,IReadOnlyCollection{MessageHeader}?,CancellationToken)" path="/*[not(self::exception)]" />
    /// <exception cref="NotSupportedException">
    ///     Always thrown, because only the overloads accepting an <see cref="IOutboundEnvelope" /> are supported.
    /// </exception>
    public ValueTask<IBrokerMessageIdentifier?> ProduceAsync(
        object? message,
        IReadOnlyCollection<MessageHeader>? headers = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException(UseEnvelopeOverloadMessage);

    /// <inheritdoc cref="IProducer.ProduceAsync(IOutboundEnvelope,CancellationToken)" />
    /// <exception cref="MissingKafkaTransactionException">
    ///     The <paramref name="envelope" /> has no context.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     No Kafka transaction was initialized in the context of the <paramref name="envelope" />, or the transaction is already bound to
    ///     another producer.
    /// </exception>
    public async ValueTask<IBrokerMessageIdentifier?> ProduceAsync(IOutboundEnvelope envelope, CancellationToken cancellationToken = default)
    {
        KafkaProducer producer = await GetProducerForTransactionAsync(Check.NotNull(envelope)).ConfigureAwait(false);
        return await producer.ProduceAsync(envelope, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc cref="IProducer.RawProduceAsync(byte[],IReadOnlyCollection{MessageHeader}?,CancellationToken)" path="/*[not(self::exception)]" />
    /// <exception cref="NotSupportedException">
    ///     Always thrown, because only the overloads accepting an <see cref="IOutboundEnvelope" /> are supported.
    /// </exception>
    public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(
        byte[]? messageContent,
        IReadOnlyCollection<MessageHeader>? headers = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException(UseEnvelopeOverloadMessage);

    /// <inheritdoc cref="IProducer.RawProduceAsync(Stream?,IReadOnlyCollection{MessageHeader}?,CancellationToken)" path="/*[not(self::exception)]" />
    /// <exception cref="NotSupportedException">
    ///     Always thrown, because only the overloads accepting an <see cref="IOutboundEnvelope" /> are supported.
    /// </exception>
    public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(
        Stream? messageStream,
        IReadOnlyCollection<MessageHeader>? headers = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException(UseEnvelopeOverloadMessage);

    private KafkaProducer GetProducerForTransaction(IOutboundEnvelope envelope) =>
        GetProducerForTransactionAsync(envelope).SafeWait();

    private async ValueTask<KafkaProducer> GetProducerForTransactionAsync(IOutboundEnvelope envelope)
    {
        KafkaTransaction transaction = envelope.Context?.GetKafkaTransaction() ?? throw new MissingKafkaTransactionException();

        KafkaProducer producer = await _transactionalProducers.GetOrCreateAsync(Name, Configuration, envelope, transaction).ConfigureAwait(false);

        transaction.BindConfluentProducer(producer.Client);
        transaction.EnsureBegin();

        return producer;
    }
}
