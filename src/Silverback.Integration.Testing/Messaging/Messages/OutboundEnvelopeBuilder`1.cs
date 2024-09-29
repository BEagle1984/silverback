// Copyright (c) 2024 Sergio Aquilini
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
/// <typeparam name="TMessage">
///     The type of the wrapped message.
/// </typeparam>
public class OutboundEnvelopeBuilder<TMessage>
    where TMessage : class
{
    private TMessage? _message;

    private MessageHeaderCollection? _headers;

    private ProducerEndpoint? _endpoint;

    private IProducer? _producer;

    /// <summary>
    ///     Sets the message to be wrapped by the envelope.
    /// </summary>
    /// <param name="message">
    ///     The message to be wrapped.
    /// </param>
    /// <returns>
    ///     The <see cref="OutboundEnvelopeBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public OutboundEnvelopeBuilder<TMessage> WithMessage(TMessage? message)
    {
        _message = message;
        return this;
    }

    /// <summary>
    ///     Sets the headers to be added to the envelope.
    /// </summary>
    /// <param name="headers">
    ///     The headers to be added.
    /// </param>
    /// <returns>
    ///     The <see cref="OutboundEnvelopeBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public OutboundEnvelopeBuilder<TMessage> WithHeaders(IReadOnlyCollection<MessageHeader>? headers)
    {
        _headers = new MessageHeaderCollection(headers);
        return this;
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
    ///     The <see cref="OutboundEnvelopeBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public OutboundEnvelopeBuilder<TMessage> AddHeader(string name, string? value)
    {
        _headers ??= [];
        _headers.Add(name, value);
        return this;
    }

    /// <summary>
    ///     Adds a header to the envelope.
    /// </summary>
    /// <param name="header">
    ///     The header to be added.
    /// </param>
    /// <returns>
    ///     The <see cref="OutboundEnvelopeBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public OutboundEnvelopeBuilder<TMessage> AddHeader(MessageHeader header)
    {
        _headers ??= [];
        _headers.Add(header);
        return this;
    }

    /// <summary>
    ///     Sets the endpoint to be used to produce the message.
    /// </summary>
    /// <param name="endpoint">
    ///     The endpoint.
    /// </param>
    /// <returns>
    ///     The <see cref="OutboundEnvelopeBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public OutboundEnvelopeBuilder<TMessage> WithEndpoint(ProducerEndpoint? endpoint)
    {
        _endpoint = endpoint;
        return this;
    }

    /// <summary>
    ///   Sets the producer to be used to produce the message.
    /// </summary>
    /// <param name="producer">
    ///  The producer.
    /// </param>
    /// <returns>
    ///  The <see cref="OutboundEnvelopeBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public OutboundEnvelopeBuilder<TMessage> WithProducer(IProducer? producer)
    {
        _producer = producer;
        return this;
    }

    /// <summary>
    ///   Builds the <see cref="IOutboundEnvelope{TMessage}" /> instance.
    /// </summary>
    /// <returns>
    ///  The <see cref="IOutboundEnvelope{TMessage}" /> instance.
    /// </returns>
    public IOutboundEnvelope<TMessage> Build() =>
        new OutboundEnvelope<TMessage>(
            _message,
            _headers,
            _endpoint ?? new MockProducerEndpoint(),
            _producer ?? new MockProducer());

    internal record MockProducerEndpoint() : ProducerEndpoint("mock", new MockProducerEndpointConfiguration());

    internal record MockProducerEndpointConfiguration : ProducerEndpointConfiguration;

    internal class MockProducer : IProducer
    {
        public string Name => "mock";

        public string DisplayName => Name;

        public ProducerEndpointConfiguration EndpointConfiguration { get; } = new MockProducerEndpointConfiguration();

        public IBrokerClient Client => throw new NotSupportedException();

        public IBrokerMessageIdentifier Produce(object? message, IReadOnlyCollection<MessageHeader>? headers = null) => throw new NotSupportedException();

        public IBrokerMessageIdentifier Produce(IOutboundEnvelope envelope) => throw new NotSupportedException();

        public void Produce(object? message, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public void Produce(IOutboundEnvelope envelope, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public IBrokerMessageIdentifier RawProduce(byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers = null) => throw new NotSupportedException();

        public IBrokerMessageIdentifier RawProduce(Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers = null) => throw new NotSupportedException();

        public IBrokerMessageIdentifier RawProduce(ProducerEndpoint endpoint, byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers = null) => throw new NotSupportedException();

        public IBrokerMessageIdentifier RawProduce(ProducerEndpoint endpoint, Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers = null) => throw new NotSupportedException();

        public void RawProduce(byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public void RawProduce(Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public void RawProduce(ProducerEndpoint endpoint, byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public void RawProduce<TState>(ProducerEndpoint endpoint, byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?, TState> onSuccess, Action<Exception, TState> onError, TState state) => throw new NotSupportedException();

        public void RawProduce(ProducerEndpoint endpoint, Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?> onSuccess, Action<Exception> onError) => throw new NotSupportedException();

        public void RawProduce<TState>(ProducerEndpoint endpoint, Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers, Action<IBrokerMessageIdentifier?, TState> onSuccess, Action<Exception, TState> onError, TState state) => throw new NotSupportedException();

        public ValueTask<IBrokerMessageIdentifier?> ProduceAsync(object? message, IReadOnlyCollection<MessageHeader>? headers = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<IBrokerMessageIdentifier?> ProduceAsync(IOutboundEnvelope envelope, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(ProducerEndpoint endpoint, byte[]? messageContent, IReadOnlyCollection<MessageHeader>? headers = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public ValueTask<IBrokerMessageIdentifier?> RawProduceAsync(ProducerEndpoint endpoint, Stream? messageStream, IReadOnlyCollection<MessageHeader>? headers = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
