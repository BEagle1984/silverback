// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Creates the <see cref="IKafkaInboundEnvelope{TMessage,TKey}" /> instances to be used for testing.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the wrapped message.
/// </typeparam>
/// <typeparam name="TKey">
///     The type of the message key.
/// </typeparam>
public class KafkaInboundEnvelopeBuilder<TMessage, TKey>
    : InboundEnvelopeBuilder<KafkaInboundEnvelopeBuilder<TMessage, TKey>, IKafkaInboundEnvelope<TMessage, TKey>, TMessage>
    where TMessage : class
{
    private TKey? _key;

    private byte[]? _rawKey;

    private DateTime? _timestamp;

    /// <inheritdoc cref="InboundEnvelopeBuilder{TBuilder,TEnvelope,TMessage}.This" />
    protected override KafkaInboundEnvelopeBuilder<TMessage, TKey> This => this;

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
    ///     The <see cref="KafkaInboundEnvelopeBuilder{TMessage,TKey}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaInboundEnvelopeBuilder<TMessage, TKey> WithOffset(string topic, int partition, long offset) =>
        WithOffset(new KafkaOffset(topic, partition, offset));

    /// <summary>
    ///     Sets the offset of the message.
    /// </summary>
    /// <param name="offset">
    ///     The offset.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaInboundEnvelopeBuilder{TMessage,TKey}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaInboundEnvelopeBuilder<TMessage, TKey> WithOffset(KafkaOffset offset) =>
        WithIdentifier(offset);

    /// <summary>
    ///     Sets the message key.
    /// </summary>
    /// <param name="key">
    ///     The message key.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaInboundEnvelopeBuilder{TMessage,TKey}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaInboundEnvelopeBuilder<TMessage, TKey> WithKey(TKey? key)
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
    ///     The <see cref="KafkaInboundEnvelopeBuilder{TMessage,TKey}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaInboundEnvelopeBuilder<TMessage, TKey> WithRawKey(byte[]? rawKey)
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
    ///     The <see cref="KafkaInboundEnvelopeBuilder{TMessage,TKey}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaInboundEnvelopeBuilder<TMessage, TKey> WithKafkaTimestamp(DateTime timestamp)
    {
        _timestamp = timestamp;
        return this;
    }

    /// <inheritdoc cref="InboundEnvelopeBuilder{TBuilder,TEnvelope,TMessage}.BuildCore" />
    protected override IKafkaInboundEnvelope<TMessage, TKey> BuildCore(
        TMessage? message,
        Stream? rawMessage,
        MessageHeaderCollection? headers,
        ConsumerEndpoint endpoint,
        IConsumer consumer,
        IBrokerMessageIdentifier identifier)
    {
        KafkaInboundEnvelope<TMessage, TKey> envelope = new(
            message,
            rawMessage,
            headers,
            endpoint,
            consumer,
            identifier);

        if (_key != null)
            envelope.SetKey(_key);

        if (_rawKey != null)
            envelope.SetRawKey(_rawKey);

        envelope.SetTimestamp(_timestamp ?? DateTime.UtcNow);

        return envelope;
    }
}
