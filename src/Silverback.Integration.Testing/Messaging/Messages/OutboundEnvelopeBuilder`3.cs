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

    private ProducerEndpointConfiguration? _endpointConfiguration;

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
    ///     Sets the endpoint to be used to produce the message.
    /// </summary>
    /// <param name="endpointConfiguration">
    ///     The endpoint configuration.
    /// </param>
    /// <returns>
    ///     The builder so that additional calls can be chained.
    /// </returns>
    public TBuilder WithEndpointConfiguration(ProducerEndpointConfiguration? endpointConfiguration)
    {
        _endpointConfiguration = endpointConfiguration;
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
    public TEnvelope Build() =>
        BuildCore(
            _message,
            _headers,
            _endpointConfiguration ?? new MockProducerEndpointConfiguration(),
            _producer ?? new MockProducer());

    /// <summary>
    ///     Builds the <see cref="IOutboundEnvelope{TMessage}" /> instance.
    /// </summary>
    /// <param name="message">
    ///     The message to be wrapped.
    /// </param>
    /// <param name="headers">
    ///     The headers to be added.
    /// </param>
    /// <param name="endpointConfiguration">
    ///     The endpoint configuration.
    /// </param>
    /// <param name="producer">
    ///     The producer.
    /// </param>
    /// <returns>
    ///     The envelope instance.
    /// </returns>
    protected abstract TEnvelope BuildCore(
        TMessage? message,
        MessageHeaderCollection? headers,
        ProducerEndpointConfiguration endpointConfiguration,
        IProducer producer);

    internal record MockProducerEndpointConfiguration : ProducerEndpointConfiguration;

    internal class MockProducer : IProducer
    {
        public string Name => "mock";

        public string DisplayName => Name;

        public ProducerEndpointConfiguration EndpointConfiguration { get; } = new MockProducerEndpointConfiguration();

        public IOutboundEnvelopeFactory EnvelopeFactory => throw new NotSupportedException();

        public IBrokerClient Client => throw new NotSupportedException();

        public IBrokerMessageIdentifier Produce(object? message, IReadOnlyCollection<MessageHeader>? headers = null) => throw new NotSupportedException();

        public IBrokerMessageIdentifier Produce(IOutboundEnvelope envelope) => throw new NotSupportedException();

        public void Produce(object? message, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public void Produce<TState>(object? message, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?, TState> onSuccess, Action<Exception, TState> onError, TState state) => throw new NotSupportedException();

        public void Produce(IOutboundEnvelope envelope, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public void Produce<TState>(IOutboundEnvelope envelope, Action<IBrokerMessageIdentifier?, TState> onSuccess, Action<Exception, TState> onError, TState state) => throw new NotSupportedException();

        public IBrokerMessageIdentifier RawProduce(byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers = null) => throw new NotSupportedException();

        public IBrokerMessageIdentifier RawProduce(Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers = null) => throw new NotSupportedException();

        public void RawProduce(byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public void RawProduce<TState>(byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?, TState> onSuccess, Action<Exception, TState> onError, TState state) => throw new NotSupportedException();

        public void RawProduce(Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public void RawProduce<TState>(Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?, TState> onSuccess, Action<Exception, TState> onError, TState state) => throw new NotSupportedException();

        public ValueTask<IBrokerMessageIdentifier?> ProduceAsync(object? message, IReadOnlyCollection<MessageHeader>? headers = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<IBrokerMessageIdentifier?> ProduceAsync(IOutboundEnvelope envelope, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(ProducerEndpoint endpoint, byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(ProducerEndpoint endpoint, Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
