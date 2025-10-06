// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Creates the <see cref="IInboundEnvelope{TMessage}" /> instances to be used for testing.
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
public abstract class InboundEnvelopeBuilder<TBuilder, TEnvelope, TMessage>
    where TBuilder : InboundEnvelopeBuilder<TBuilder, TEnvelope, TMessage>
    where TEnvelope : IInboundEnvelope<TMessage>
    where TMessage : class
{
    private Stream? _rawMessage;

    private TMessage? _message;

    private MessageHeaderCollection? _headers;

    private ConsumerEndpoint? _endpoint;

    private IConsumer? _consumer;

    private IBrokerMessageIdentifier? _identifier;

    /// <summary>
    ///     Gets this instance.
    /// </summary>
    /// <remarks>
    ///     This is necessary to work around casting in the base classes.
    /// </remarks>
    protected abstract TBuilder This { get; }

    /// <summary>
    ///     Sets the raw message to be wrapped by the envelope.
    /// </summary>
    /// <param name="rawMessage">
    ///     The raw message to be wrapped.
    /// </param>
    /// <returns>
    ///     The builder so that additional calls can be chained.
    /// </returns>
    public TBuilder WithRawMessage(Stream? rawMessage)
    {
        _rawMessage = rawMessage;
        return This;
    }

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
    public TBuilder AddHeader(string name, object? value)
    {
        _headers ??= [];
        _headers.Add(name, value);
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
    public TBuilder AddOrReplaceHeader(string name, object? value)
    {
        _headers ??= [];
        _headers.AddOrReplace(name, value);
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
    public TBuilder AddOrReplaceHeader(string name, string? value)
    {
        _headers ??= [];
        _headers.AddOrReplace(name, value);
        return This;
    }

    /// <summary>
    ///     Sets the endpoint from which the message was consumed.
    /// </summary>
    /// <param name="endpoint">
    ///     The endpoint.
    /// </param>
    /// <returns>
    ///     The builder so that additional calls can be chained.
    /// </returns>
    public TBuilder WithEndpoint(ConsumerEndpoint? endpoint)
    {
        _endpoint = endpoint;
        return This;
    }

    /// <summary>
    ///     Sets the consumer that consumed the message.
    /// </summary>
    /// <param name="consumer">
    ///     The consumer.
    /// </param>
    /// <returns>
    ///     The builder so that additional calls can be chained.
    /// </returns>
    public TBuilder WithConsumer(IConsumer? consumer)
    {
        _consumer = consumer;
        return This;
    }

    /// <summary>
    ///     Sets the identifier of the message.
    /// </summary>
    /// <param name="identifier">
    ///     The identifier.
    /// </param>
    /// <returns>
    ///     The builder so that additional calls can be chained.
    /// </returns>
    public TBuilder WithIdentifier(IBrokerMessageIdentifier identifier)
    {
        _identifier = identifier;
        return This;
    }

    /// <summary>
    ///     Builds the <see cref="IInboundEnvelope{TMessage}" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="IInboundEnvelope{TMessage}" /> instance.
    /// </returns>
    public TEnvelope Build() =>
        BuildCore(
            _message,
            _rawMessage,
            _headers,
            _endpoint ?? new MockConsumerEndpoint(),
            _consumer ?? new MockConsumer(),
            _identifier);

    /// <summary>
    ///     Builds the <see cref="IOutboundEnvelope{TMessage}" /> instance.
    /// </summary>
    /// <param name="message">
    ///     The message to be wrapped.
    /// </param>
    /// <param name="rawMessage">
    ///     The raw message to be wrapped.
    /// </param>
    /// <param name="headers">
    ///     The headers to be added.
    /// </param>
    /// <param name="endpoint">
    ///     The endpoint.
    /// </param>
    /// <param name="consumer">
    ///     The consumer.
    /// </param>
    /// <param name="identifier">
    ///     The <see cref="IBrokerMessageIdentifier" />.
    /// </param>
    /// <returns>
    ///     The envelope instance.
    /// </returns>
    protected abstract TEnvelope BuildCore(
        TMessage? message,
        Stream? rawMessage,
        MessageHeaderCollection? headers,
        ConsumerEndpoint endpoint,
        IConsumer consumer,
        IBrokerMessageIdentifier? identifier);

    internal record MockConsumerEndpoint() : ConsumerEndpoint("mock", new MockConsumerEndpointConfiguration());

    internal record MockConsumerEndpointConfiguration : ConsumerEndpointConfiguration;

    internal class MockConsumer : IConsumer
    {
        public string Name => "mock";

        public string DisplayName => Name;

        public IBrokerClient Client => throw new NotSupportedException();

        public IReadOnlyCollection<ConsumerEndpointConfiguration> EndpointsConfiguration { get; } = [new MockConsumerEndpointConfiguration()];

        public IInboundEnvelopeFactory EnvelopeFactory => throw new NotSupportedException();

        public IConsumerStatusInfo StatusInfo => throw new NotSupportedException();

        public ValueTask TriggerReconnectAsync() => throw new NotSupportedException();

        public ValueTask StartAsync() => throw new NotSupportedException();

        public ValueTask StopAsync(bool waitUntilStopped = true) => throw new NotSupportedException();

        public ValueTask CommitAsync(IBrokerMessageIdentifier brokerMessageIdentifier) => throw new NotSupportedException();

        public ValueTask CommitAsync(IReadOnlyCollection<IBrokerMessageIdentifier> brokerMessageIdentifiers) => throw new NotSupportedException();

        public ValueTask RollbackAsync(IBrokerMessageIdentifier brokerMessageIdentifier) => throw new NotSupportedException();

        public ValueTask RollbackAsync(IReadOnlyCollection<IBrokerMessageIdentifier> brokerMessageIdentifiers) => throw new NotSupportedException();

        public int IncrementFailedAttempts(IInboundEnvelope envelope) => throw new NotSupportedException();
    }
}
