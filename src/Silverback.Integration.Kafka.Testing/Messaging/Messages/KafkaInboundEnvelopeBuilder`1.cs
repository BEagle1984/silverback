// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Creates the <see cref="IKafkaInboundEnvelope{TMessage}" /> instances to be used for testing.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the wrapped message.
/// </typeparam>
public class KafkaInboundEnvelopeBuilder<TMessage>
    : InboundEnvelopeBuilder<KafkaInboundEnvelopeBuilder<TMessage>, IKafkaInboundEnvelope<TMessage>, TMessage>
    where TMessage : class
{
    private object? _key;

    private byte[]? _rawKey;

    private DateTime? _timestamp;

    /// <inheritdoc cref="InboundEnvelopeBuilder{TBuilder,TEnvelope,TMessage}.This" />
    protected override KafkaInboundEnvelopeBuilder<TMessage> This => this;

    /// <summary>
    ///     Sets the offset of the message.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the message.
    /// </typeparam>
    /// <param name="topic">
    ///     The topic.
    /// </param>
    /// <param name="partition">
    ///     The partition.
    /// </param>
    /// <param name="offset">
    ///     The offset.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaInboundEnvelopeBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaInboundEnvelopeBuilder<TMessage> WithOffset(string topic, int partition, long offset) =>
        WithOffset(new KafkaOffset(topic, partition, offset));

    /// <summary>
    ///     Sets the offset of the message.
    /// </summary>
    /// <param name="offset">
    ///     The offset.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaInboundEnvelopeBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaInboundEnvelopeBuilder<TMessage> WithOffset(KafkaOffset offset) =>
        WithIdentifier(offset);

    /// <summary>
    ///     Sets the message key.
    /// </summary>
    /// <param name="key">
    ///     The message key.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaInboundEnvelopeBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaInboundEnvelopeBuilder<TMessage> WithKey(object? key)
    {
        _key = key;
        return this;
    }

    /// <summary>
    ///     Sets the serialized message key.
    /// </summary>
    /// <param name="rawKey">
    ///     The serialized message key.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaInboundEnvelopeBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaInboundEnvelopeBuilder<TMessage> WithRawKey(byte[]? rawKey)
    {
        _rawKey = rawKey;
        return this;
    }

    /// <summary>
    ///     Sets the message timestamp.
    /// </summary>
    /// <param name="timestamp">
    ///     The message timestamp.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaInboundEnvelopeBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaInboundEnvelopeBuilder<TMessage> WithTimestamp(DateTime timestamp)
    {
        _timestamp = timestamp;
        return this;
    }

    /// <inheritdoc cref="InboundEnvelopeBuilder{TBuilder,TEnvelope,TMessage}.BuildCore" />
    protected override IKafkaInboundEnvelope<TMessage> BuildCore(
        TMessage? message,
        Stream? rawMessage,
        MessageHeaderCollection? headers,
        ConsumerEndpoint endpoint,
        IConsumer consumer,
        IBrokerMessageIdentifier? identifier)
    {
        Check.NotNull(endpoint, nameof(endpoint));

        KafkaInboundEnvelope<TMessage> envelope = new(
            message,
            rawMessage,
            endpoint,
            consumer,
            identifier ?? new KafkaOffset(endpoint.RawName, 0, 0));

        if (headers != null)
            envelope.Headers.AddRange(headers);

        if (_key != null)
            envelope.SetKey(_key);

        if (_rawKey != null)
            envelope.SetRawKey(_rawKey);

        envelope.SetTimestamp(_timestamp ?? DateTime.UtcNow);

        return envelope;
    }
}
