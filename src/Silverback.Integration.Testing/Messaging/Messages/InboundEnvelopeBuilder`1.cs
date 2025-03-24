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
/// <typeparam name="TMessage">
///     The type of the wrapped message.
/// </typeparam>
public class InboundEnvelopeBuilder<TMessage>
    where TMessage : class
{
    private Stream? _rawMessage;

    private TMessage? _message;

    private MessageHeaderCollection? _headers;

    private ConsumerEndpoint? _endpoint;

    private IConsumer? _consumer;

    private IBrokerMessageIdentifier? _identifier;

    /// <summary>
    ///     Sets the raw message to be wrapped by the envelope.
    /// </summary>
    /// <param name="rawMessage">
    ///     The raw message to be wrapped.
    /// </param>
    /// <returns>
    ///     The <see cref="InboundEnvelopeBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public InboundEnvelopeBuilder<TMessage> WithRawMessage(Stream? rawMessage)
    {
        _rawMessage = rawMessage;
        return this;
    }

    /// <summary>
    ///     Sets the message to be wrapped by the envelope.
    /// </summary>
    /// <param name="message">
    ///     The message to be wrapped.
    /// </param>
    /// <returns>
    ///     The <see cref="InboundEnvelopeBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public InboundEnvelopeBuilder<TMessage> WithMessage(TMessage? message)
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
    ///     The <see cref="InboundEnvelopeBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public InboundEnvelopeBuilder<TMessage> WithHeaders(IReadOnlyCollection<MessageHeader>? headers)
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
    ///     The <see cref="InboundEnvelopeBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public InboundEnvelopeBuilder<TMessage> AddHeader(string name, object? value)
    {
        _headers ??= [];
        _headers.Add(name, value);
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
    ///     The <see cref="InboundEnvelopeBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public InboundEnvelopeBuilder<TMessage> AddHeader(string name, string? value)
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
    ///     The <see cref="InboundEnvelopeBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public InboundEnvelopeBuilder<TMessage> AddHeader(MessageHeader header)
    {
        _headers ??= [];
        _headers.Add(header);
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
    ///     The <see cref="InboundEnvelopeBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public InboundEnvelopeBuilder<TMessage> AddOrReplaceHeader(string name, object? value)
    {
        _headers ??= [];
        _headers.AddOrReplace(name, value);
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
    ///     The <see cref="InboundEnvelopeBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public InboundEnvelopeBuilder<TMessage> AddOrReplaceHeader(string name, string? value)
    {
        _headers ??= [];
        _headers.AddOrReplace(name, value);
        return this;
    }

    /// <summary>
    ///     Sets the endpoint from which the message was consumed.
    /// </summary>
    /// <param name="endpoint">
    ///     The endpoint.
    /// </param>
    /// <returns>
    ///     The <see cref="InboundEnvelopeBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public InboundEnvelopeBuilder<TMessage> WithEndpoint(ConsumerEndpoint? endpoint)
    {
        _endpoint = endpoint;
        return this;
    }

    /// <summary>
    ///     Sets the consumer that consumed the message.
    /// </summary>
    /// <param name="consumer">
    ///     The consumer.
    /// </param>
    /// <returns>
    ///     The <see cref="InboundEnvelopeBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public InboundEnvelopeBuilder<TMessage> WithConsumer(IConsumer? consumer)
    {
        _consumer = consumer;
        return this;
    }

    /// <summary>
    ///     Sets the identifier of the message.
    /// </summary>
    /// <param name="identifier">
    ///     The identifier.
    /// </param>
    /// <returns>
    ///     The <see cref="InboundEnvelopeBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public InboundEnvelopeBuilder<TMessage> WithIdentifier(IBrokerMessageIdentifier identifier)
    {
        _identifier = identifier;
        return this;
    }

    /// <summary>
    ///     Builds the <see cref="IInboundEnvelope{TMessage}" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="IInboundEnvelope{TMessage}" /> instance.
    /// </returns>
    public IInboundEnvelope<TMessage> Build() =>
        new InboundEnvelope<TMessage>(
            _message,
            _rawMessage,
            _headers,
            _endpoint ?? new MockConsumerEndpoint(),
            _consumer ?? new MockConsumer(),
            _identifier ?? new MockBrokerMessageIdentifier());

    internal record MockConsumerEndpoint() : ConsumerEndpoint("mock", new MockConsumerEndpointConfiguration());

    internal record MockConsumerEndpointConfiguration : ConsumerEndpointConfiguration;

    internal class MockConsumer : IConsumer
    {
        public string Name => "mock";

        public string DisplayName => Name;

        public IBrokerClient Client => throw new NotSupportedException();

        public IReadOnlyCollection<ConsumerEndpointConfiguration> EndpointsConfiguration { get; } = [new MockConsumerEndpointConfiguration()];

        public IConsumerStatusInfo StatusInfo => throw new NotSupportedException();

        public ValueTask TriggerReconnectAsync() => throw new NotSupportedException();

        public ValueTask StartAsync() => throw new NotSupportedException();

        public ValueTask StopAsync(bool waitUntilStopped = true) => throw new NotSupportedException();

        public ValueTask CommitAsync(IBrokerMessageIdentifier brokerMessageIdentifier) => throw new NotSupportedException();

        public ValueTask CommitAsync(IReadOnlyCollection<IBrokerMessageIdentifier> brokerMessageIdentifiers) => throw new NotSupportedException();

        public ValueTask RollbackAsync(IBrokerMessageIdentifier brokerMessageIdentifier) => throw new NotSupportedException();

        public ValueTask RollbackAsync(IReadOnlyCollection<IBrokerMessageIdentifier> brokerMessageIdentifiers) => throw new NotSupportedException();

        public int IncrementFailedAttempts(IRawInboundEnvelope envelope) => throw new NotSupportedException();
    }

    internal class MockBrokerMessageIdentifier : IBrokerMessageIdentifier
    {
        public bool Equals(IBrokerMessageIdentifier? other) => this == other;

        public string ToLogString() => "mock";

        public string ToVerboseLogString() => "mock";
    }
}
