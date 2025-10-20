// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Creates the <see cref="IOutboundEnvelope{TMessage}" /> instances to be used for testing.
/// </summary>
/// <typeparam name="TBuilder">
///     The type of the actual builder.
/// </typeparam>
/// <typeparam name="TEnvelope">
///     The type of the envelope being built.
/// </typeparam>
/// <typeparam name="TMessage">
///     The type of the wrapped message.
/// </typeparam>
public abstract class OutboundEnvelopeBuilder<TBuilder, TEnvelope, TMessage>
    where TBuilder : OutboundEnvelopeBuilder<TBuilder, TEnvelope, TMessage>
    where TEnvelope : IOutboundEnvelope<TMessage>
    where TMessage : class
{
    private TMessage? _message;

    private MessageHeaderCollection? _headers;

    private IProducer? _producer;

    /// <summary>
    ///     Gets this instance.
    /// </summary>
    /// <remarks>
    ///     This is necessary to work around casting in the base classes.
    /// </remarks>
    protected abstract TBuilder This { get; }

    /// <summary>
    ///     Sets the message to be wrapped by the envelope.
    /// </summary>
    /// <param name="message">
    ///     The message to be wrapped.
    /// </param>
    /// <returns>
    ///     The builder so that additional calls can be chained.
    /// </returns>
    public TBuilder WithMessage(TMessage? message)
    {
        _message = message;
        return This;
    }

    /// <summary>
    ///     Sets the headers to be added to the envelope.
    /// </summary>
    /// <param name="headers">
    ///     The headers to be added.
    /// </param>
    /// <returns>
    ///     The builder so that additional calls can be chained.
    /// </returns>
    public TBuilder WithHeaders(IReadOnlyCollection<MessageHeader>? headers)
    {
        _headers = new MessageHeaderCollection(headers);
        return This;
    }

    /// <summary>
    ///     Adds a header to the envelope.
    /// </summary>
    /// <param name="name">
    ///     The name of the header.
    /// </param>
    /// <param name="value">
    ///     The value of the header.
    /// </param>
    /// <returns>
    ///     The builder so that additional calls can be chained.
    /// </returns>
    public TBuilder AddHeader(string name, string? value)
    {
        _headers ??= [];
        _headers.Add(name, value);
        return This;
    }

    /// <summary>
    ///     Adds a header to the envelope.
    /// </summary>
    /// <param name="header">
    ///     The header to be added.
    /// </param>
    /// <returns>
    ///     The builder so that additional calls can be chained.
    /// </returns>
    public TBuilder AddHeader(MessageHeader header)
    {
        _headers ??= [];
        _headers.Add(header);
        return This;
    }

    /// <summary>
    ///     Sets the producer to be used to produce the message.
    /// </summary>
    /// <param name="producer">
    ///     The producer.
    /// </param>
    /// <returns>
    ///     The builder so that additional calls can be chained.
    /// </returns>
    public TBuilder WithProducer(IProducer? producer)
    {
        _producer = producer;
        return This;
    }

    /// <summary>
    ///     Builds the <see cref="IOutboundEnvelope{TMessage}" /> instance.
    /// </summary>
    /// <returns>
    ///     The envelope instance.
    /// </returns>
    public TEnvelope Build()
    {
        TEnvelope envelope = BuildCore(_message, _producer ?? new MockProducer());

        if (_headers != null)
            envelope.Headers.AddRange(_headers);

        return envelope;
    }

    /// <summary>
    ///     Builds the <see cref="IOutboundEnvelope{TMessage}" /> instance.
    /// </summary>
    /// <param name="message">
    ///     The message to be wrapped.
    /// </param>
    /// <param name="producer">
    ///     The producer.
    /// </param>
    /// <returns>
    ///     The envelope instance.
    /// </returns>
    protected abstract TEnvelope BuildCore(TMessage? message, IProducer producer);

    internal record MockProducerEndpointConfiguration : ProducerEndpointConfiguration;

    internal class MockProducer : IProducer
    {
        public string Name => "mock";

        public string DisplayName => Name;

        public ProducerEndpointConfiguration EndpointConfiguration { get; } = new MockProducerEndpointConfiguration();

        public IOutboundEnvelopeFactory EnvelopeFactory => throw new NotSupportedException();

        public IBrokerClient Client => throw new NotSupportedException();

        public IBrokerMessageIdentifier Produce<TMessage1>(TMessage1? message, Action<IOutboundEnvelope<TMessage1>>? envelopeConfigurationAction = null)
            where TMessage1 : class => throw new NotSupportedException();

        public IBrokerMessageIdentifier Produce(IOutboundEnvelope envelope) => throw new NotSupportedException();

        public void Produce<TMessage1>(TMessage1? message, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError, Action<IOutboundEnvelope<TMessage1>>? envelopeConfigurationAction = null)
            where TMessage1 : class => throw new NotSupportedException();

        public void Produce<TMessage1, TState>(TMessage1? message, Action<IBrokerMessageIdentifier?, TState> onSuccess, Action<Exception, TState> onError, TState state, Action<IOutboundEnvelope<TMessage1>>? envelopeConfigurationAction = null) where TMessage1 : class
            => throw new NotSupportedException();

        public void Produce(IOutboundEnvelope envelope, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public void Produce<TState>(IOutboundEnvelope envelope, Action<IBrokerMessageIdentifier?, TState> onSuccess, Action<Exception, TState> onError, TState state) => throw new NotSupportedException();

        public IBrokerMessageIdentifier RawProduce(byte[]? messageContent, Action<IOutboundEnvelope>? envelopeConfigurationAction = null) => throw new NotSupportedException();

        public IBrokerMessageIdentifier RawProduce(Stream? messageStream, Action<IOutboundEnvelope>? envelopeConfigurationAction = null) => throw new NotSupportedException();

        public IBrokerMessageIdentifier RawProduce(IOutboundEnvelope envelope) => throw new NotSupportedException();

        public void RawProduce(byte[]? messageContent, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError, Action<IOutboundEnvelope>? envelopeConfigurationAction = null) => throw new NotSupportedException();

        public void RawProduce<TState>(byte[]? messageContent, Action<IBrokerMessageIdentifier?, TState> onSuccess, Action<Exception, TState> onError, TState state, Action<IOutboundEnvelope>? envelopeConfigurationAction = null) => throw new NotSupportedException();

        public void RawProduce(Stream? messageStream, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError, Action<IOutboundEnvelope>? envelopeConfigurationAction = null) => throw new NotSupportedException();

        public void RawProduce<TState>(Stream? messageStream, Action<IBrokerMessageIdentifier?, TState> onSuccess, Action<Exception, TState> onError, TState state, Action<IOutboundEnvelope>? envelopeConfigurationAction = null) => throw new NotSupportedException();

        public void RawProduce(IOutboundEnvelope envelope, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public void RawProduce<TState>(IOutboundEnvelope envelope, Action<IBrokerMessageIdentifier?, TState> onSuccess, Action<Exception, TState> onError, TState state) => throw new NotSupportedException();

        public ValueTask<IBrokerMessageIdentifier?> ProduceAsync(object? message, Action<IOutboundEnvelope>? envelopeConfigurationAction = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<IBrokerMessageIdentifier?> ProduceAsync(IOutboundEnvelope envelope, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(byte[]? messageContent, Action<IOutboundEnvelope>? envelopeConfigurationAction = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(Stream? messageStream, Action<IOutboundEnvelope>? envelopeConfigurationAction = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(IOutboundEnvelope envelope, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
