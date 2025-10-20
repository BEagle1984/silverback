// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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

        EnvelopeFactory = new KafkaOutboundEnvelopeFactory<string>(this);
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

    /// <inheritdoc cref="IProducer.EnvelopeFactory" />
    public IOutboundEnvelopeFactory EnvelopeFactory { get; }

    /// <inheritdoc cref="IProducer.Produce{TMessage}(TMessage, Action{IOutboundEnvelope{TMessage}})" />
    public IBrokerMessageIdentifier Produce<TMessage>(TMessage? message, Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null)
        where TMessage : class =>
        throw new NotSupportedException("Use the overload accepting an IOutboundEnvelope instead.");

    /// <inheritdoc cref="IProducer.Produce(IOutboundEnvelope)" />
    public IBrokerMessageIdentifier? Produce(IOutboundEnvelope envelope) =>
        GetProducerForTransaction(Check.NotNull(envelope, nameof(envelope))).Produce(envelope);

    /// <inheritdoc cref="IProducer.Produce{TMessage}(TMessage, Action{IBrokerMessageIdentifier?}, Action{Exception}, Action{IOutboundEnvelope{TMessage}})" />
    public void Produce<TMessage>(TMessage? message, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError, Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null)
        where TMessage : class =>
        throw new NotSupportedException("Use the overload accepting an IOutboundEnvelope instead.");

    /// <inheritdoc cref="IProducer.Produce{TMessage,TState}(TMessage, Action{IBrokerMessageIdentifier?, TState}, Action{Exception, TState}, TState, Action{IOutboundEnvelope{TMessage}})" />
    public void Produce<TMessage, TState>(TMessage? message, Action<IBrokerMessageIdentifier?, TState> onSuccess, Action<Exception, TState> onError, TState state, Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null)
        where TMessage : class =>
        throw new NotSupportedException("Use the overload accepting an IOutboundEnvelope instead.");

    /// <inheritdoc cref="IProducer.Produce(IOutboundEnvelope, Action{IBrokerMessageIdentifier?}, Action{Exception})" />
    public void Produce(IOutboundEnvelope envelope, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) =>
        GetProducerForTransaction(Check.NotNull(envelope, nameof(envelope))).Produce(envelope, onSuccess, onError);

    /// <inheritdoc cref="IProducer.Produce{TState}(IOutboundEnvelope, Action{IBrokerMessageIdentifier?, TState}, Action{Exception, TState}, TState)" />
    public void Produce<TState>(IOutboundEnvelope envelope, Action<IBrokerMessageIdentifier?, TState> onSuccess, Action<Exception, TState> onError, TState state) =>
        GetProducerForTransaction(Check.NotNull(envelope, nameof(envelope))).Produce(envelope, onSuccess, onError, state);

    /// <inheritdoc cref="IProducer.RawProduce(byte[], Action{IOutboundEnvelope}?)" />
    public IBrokerMessageIdentifier RawProduce(byte[]? messageContent, Action<IOutboundEnvelope>? envelopeConfigurationAction = null) =>
        throw new NotSupportedException("RawProduce is not supported.");

    /// <inheritdoc cref="IProducer.RawProduce(Stream?, Action{IOutboundEnvelope}?)" />
    public IBrokerMessageIdentifier RawProduce(Stream? messageStream, Action<IOutboundEnvelope>? envelopeConfigurationAction = null) =>
        throw new NotSupportedException("RawProduce is not supported.");

    /// <inheritdoc cref="IProducer.RawProduce(IOutboundEnvelope)" />
    public IBrokerMessageIdentifier RawProduce(IOutboundEnvelope envelope) =>
        throw new NotSupportedException("RawProduce is not supported.");

    /// <inheritdoc cref="IProducer.RawProduce(byte[], Action{IBrokerMessageIdentifier?}, Action{Exception}, Action{IOutboundEnvelope}?)" />
    public void RawProduce(byte[]? messageContent, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError, Action<IOutboundEnvelope>? envelopeConfigurationAction = null) =>
        throw new NotSupportedException("RawProduce is not supported.");

    /// <inheritdoc cref="IProducer.RawProduce{TState}(byte[], Action{IBrokerMessageIdentifier?, TState}, Action{Exception, TState}, TState, Action{IOutboundEnvelope}?)" />
    public void RawProduce<TState>(byte[]? messageContent, Action<IBrokerMessageIdentifier?, TState> onSuccess, Action<Exception, TState> onError, TState state, Action<IOutboundEnvelope>? envelopeConfigurationAction = null) =>
        throw new NotSupportedException("RawProduce is not supported.");

    /// <inheritdoc cref="IProducer.RawProduce(Stream?, Action{IBrokerMessageIdentifier?}, Action{Exception}, Action{IOutboundEnvelope}?)" />
    public void RawProduce(Stream? messageStream, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError, Action<IOutboundEnvelope>? envelopeConfigurationAction = null) =>
        throw new NotSupportedException("RawProduce is not supported.");

    /// <inheritdoc cref="IProducer.RawProduce{TState}(Stream?, Action{IBrokerMessageIdentifier?, TState}, Action{Exception, TState}, TState, Action{IOutboundEnvelope}?)" />
    public void RawProduce<TState>(Stream? messageStream, Action<IBrokerMessageIdentifier?, TState> onSuccess, Action<Exception, TState> onError, TState state, Action<IOutboundEnvelope>? envelopeConfigurationAction = null) =>
        throw new NotSupportedException("RawProduce is not supported.");

    /// <inheritdoc cref="IProducer.RawProduce(IOutboundEnvelope, Action{IBrokerMessageIdentifier?}, Action{Exception})" />
    public void RawProduce(IOutboundEnvelope envelope, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) =>
        throw new NotSupportedException("RawProduce is not supported.");

    /// <inheritdoc cref="IProducer.RawProduce{TState}(IOutboundEnvelope, Action{IBrokerMessageIdentifier?, TState}, Action{Exception, TState}, TState)" />
    public void RawProduce<TState>(IOutboundEnvelope envelope, Action<IBrokerMessageIdentifier?, TState> onSuccess, Action<Exception, TState> onError, TState state) =>
        throw new NotSupportedException("Use the overload accepting an IOutboundEnvelope instead.");

    /// <inheritdoc cref="IProducer.ProduceAsync(object?, Action{IOutboundEnvelope}?, CancellationToken)" />
    public ValueTask<IBrokerMessageIdentifier?> ProduceAsync(object? message, Action<IOutboundEnvelope>? envelopeConfigurationAction = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Use the overload accepting an IOutboundEnvelope instead.");

    /// <inheritdoc cref="IProducer.ProduceAsync(IOutboundEnvelope,CancellationToken)" />
    public async ValueTask<IBrokerMessageIdentifier?> ProduceAsync(IOutboundEnvelope envelope, CancellationToken cancellationToken = default)
    {
        KafkaProducer producer = await GetProducerForTransactionAsync(Check.NotNull(envelope, nameof(envelope))).ConfigureAwait(false);
        return await producer.ProduceAsync(envelope, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc cref="IProducer.RawProduceAsync(byte[], Action{IOutboundEnvelope}?, CancellationToken)" />
    public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(byte[]? messageContent, Action<IOutboundEnvelope>? envelopeConfigurationAction = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("RawProduce is not supported.");

    /// <inheritdoc cref="IProducer.RawProduceAsync(Stream?, Action{IOutboundEnvelope}?, CancellationToken)" />
    public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(Stream? messageStream, Action<IOutboundEnvelope>? envelopeConfigurationAction = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("RawProduce is not supported.");

    /// <inheritdoc cref="IProducer.RawProduceAsync(IOutboundEnvelope, CancellationToken)" />
    public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(IOutboundEnvelope envelope, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("RawProduce is not supported.");

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
