// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Creates the <see cref="IKafkaOutboundEnvelope{TMessage,TKey}" /> instances to be used for testing.
/// </summary>
/// <typeparam name="TMessage">
///     The type of the wrapped message.
/// </typeparam>
/// <typeparam name="TKey">
///     The type of the message key.
/// </typeparam>
public class KafkaOutboundEnvelopeBuilder<TMessage, TKey>
    : OutboundEnvelopeBuilder<KafkaOutboundEnvelopeBuilder<TMessage, TKey>, IKafkaOutboundEnvelope<TMessage, TKey>, TMessage>
    where TMessage : class
{
    private TKey? _key;

    private byte[]? _rawKey;

    private KafkaOffset? _offset;

    private string? _dynamicDestinationTopic;

    private int? _dynamicDestinationPartition;

    private ISilverbackContext? _context;

    /// <inheritdoc cref="OutboundEnvelopeBuilder{TBuilder,TEnvelope,TMessage}.This" />
    protected override KafkaOutboundEnvelopeBuilder<TMessage, TKey> This => this;

    /// <summary>
    ///     Sets the message key.
    /// </summary>
    /// <param name="key">
    ///     The message key.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaOutboundEnvelopeBuilder{TMessage,TKey}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaOutboundEnvelopeBuilder<TMessage, TKey> WithKey(TKey? key)
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
    ///     The <see cref="KafkaOutboundEnvelopeBuilder{TMessage,TKey}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaOutboundEnvelopeBuilder<TMessage, TKey> WithRawKey(byte[]? rawKey)
    {
        _rawKey = rawKey;
        return this;
    }

    /// <summary>
    ///     Sets the offset.
    /// </summary>
    /// <param name="offset">
    ///     The offset.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaOutboundEnvelopeBuilder{TMessage,TKey}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaOutboundEnvelopeBuilder<TMessage, TKey> WithOffset(KafkaOffset? offset)
    {
        _offset = offset;
        return this;
    }

    /// <summary>
    ///     Sets the dynamic destination topic.
    /// </summary>
    /// <param name="topic">
    ///     The dynamic destination topic.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaOutboundEnvelopeBuilder{TMessage,TKey}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaOutboundEnvelopeBuilder<TMessage, TKey> WithDestinationTopic(string? topic)
    {
        _dynamicDestinationTopic = topic;
        return this;
    }

    /// <summary>
    ///     Sets the dynamic destination partition.
    /// </summary>
    /// <param name="partition">
    ///     The dynamic destination partition.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaOutboundEnvelopeBuilder{TMessage,TKey}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaOutboundEnvelopeBuilder<TMessage, TKey> WithDestinationPartition(int? partition)
    {
        _dynamicDestinationPartition = partition;
        return this;
    }

    /// <summary>
    ///     Sets the Silverback context.
    /// </summary>
    /// <param name="context">
    ///     The <see cref="ISilverbackContext" />.
    /// </param>
    /// <returns>
    ///     The <see cref="KafkaOutboundEnvelopeBuilder{TMessage,TKey}" /> so that additional calls can be chained.
    /// </returns>
    public KafkaOutboundEnvelopeBuilder<TMessage, TKey> WithContext(ISilverbackContext? context)
    {
        _context = context;
        return this;
    }

    /// <inheritdoc cref="OutboundEnvelopeBuilder{TBuilder,TEnvelope,TMessage}.BuildCore" />
    protected override IKafkaOutboundEnvelope<TMessage, TKey> BuildCore(TMessage? message, IProducer producer)
    {
        KafkaOutboundEnvelope<TMessage, TKey> envelope = new(message, producer, _context);

        if (_key != null)
            envelope.SetKey(_key);

        if (_rawKey != null)
            envelope.SetRawKey(_rawKey);

        if (_offset != null)
            envelope.SetOffset(_offset);

        if (_dynamicDestinationTopic != null)
            envelope.SetDestinationTopic(_dynamicDestinationTopic);

        if (_dynamicDestinationPartition != null)
            envelope.SetDestinationPartition(_dynamicDestinationPartition);

        return envelope;
    }
}
