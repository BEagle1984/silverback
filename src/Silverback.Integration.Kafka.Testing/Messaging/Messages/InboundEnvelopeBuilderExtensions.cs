// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Adds the <c>WithOffset</c> methods to the <see cref="InboundEnvelopeBuilder{TMessage}" />.
/// </summary>
public static class InboundEnvelopeBuilderExtensions
{
    /// <summary>
    ///     Sets the offset of the message.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the message.
    /// </typeparam>
    /// <param name="builder">
    ///     The <see cref="InboundEnvelopeBuilder{TMessage}" />.
    /// </param>
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
    ///     The <see cref="InboundEnvelopeBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public static InboundEnvelopeBuilder<TMessage> WithOffset<TMessage>(
        this InboundEnvelopeBuilder<TMessage> builder,
        string topic,
        int partition,
        long offset)
        where TMessage : class =>
        WithOffset(builder, new KafkaOffset(topic, partition, offset));

    /// <summary>
    ///     Sets the offset of the message.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the message.
    /// </typeparam>
    /// <param name="builder">
    ///     The <see cref="InboundEnvelopeBuilder{TMessage}" />.
    /// </param>
    /// <param name="offset">
    ///     The offset.
    /// </param>
    /// <returns>
    ///     The <see cref="InboundEnvelopeBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public static InboundEnvelopeBuilder<TMessage> WithOffset<TMessage>(this InboundEnvelopeBuilder<TMessage> builder, KafkaOffset offset)
        where TMessage : class =>
        Check.NotNull(builder, nameof(builder)).WithIdentifier(offset);

    /// <summary>
    ///     Sets the key of the message consumed from Kafka.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the message.
    /// </typeparam>
    /// <param name="builder">
    ///     The <see cref="InboundEnvelopeBuilder{TMessage}" />.
    /// </param>
    /// <param name="key">
    ///     The Kafka message key.
    /// </param>
    /// <returns>
    ///     The <see cref="InboundEnvelopeBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public static InboundEnvelopeBuilder<TMessage> WithKafkaKey<TMessage>(this InboundEnvelopeBuilder<TMessage> builder, string key)
        where TMessage : class =>
        Check.NotNull(builder, nameof(builder)).AddHeader(DefaultMessageHeaders.MessageId, key);

    /// <summary>
    ///     Sets the timestamp of the message consumed from Kafka.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the message.
    /// </typeparam>
    /// <param name="builder">
    ///     The <see cref="InboundEnvelopeBuilder{TMessage}" />.
    /// </param>
    /// <param name="timestamp">
    ///     The timestamp of the message.
    /// </param>
    /// <returns>
    ///     The <see cref="InboundEnvelopeBuilder{TMessage}" /> so that additional calls can be chained.
    /// </returns>
    public static InboundEnvelopeBuilder<TMessage> WithKafkaTimestamp<TMessage>(
        this InboundEnvelopeBuilder<TMessage> builder,
        DateTime timestamp)
        where TMessage : class =>
        Check.NotNull(builder, nameof(builder)).AddHeader(KafkaMessageHeaders.Timestamp, timestamp);
}
