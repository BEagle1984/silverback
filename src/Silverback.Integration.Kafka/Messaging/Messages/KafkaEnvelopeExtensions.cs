// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Adds some convenience extension methods to the envelope interfaces.
/// </summary>
public static class KafkaEnvelopeExtensions
{
    /// <summary>
    ///     Gets the key of the message consumed from Kafka.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <returns>
    ///     The Kafka message key.
    /// </returns>
    public static string? GetKafkaKey(this IBrokerEnvelope envelope) =>
        Check.NotNull(envelope, nameof(envelope)).Headers.GetValue(DefaultMessageHeaders.MessageId);

    /// <summary>
    ///     Set the key of the message to be produced to Kafka.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <param name="key">
    ///     The Kafka message key.
    /// </param>
    /// <returns>
    ///     The <see cref="IRawOutboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    public static IRawOutboundEnvelope SetKafkaKey(this IRawOutboundEnvelope envelope, string key)
    {
        Check.NotNull(envelope, nameof(envelope));
        envelope.SetMessageId(key);
        return envelope;
    }

    /// <summary>
    ///     Gets the timestamp of the message consumed from Kafka.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <returns>
    ///     The timestamp of the message.
    /// </returns>
    public static DateTime? GetKafkaTimestamp(this IRawInboundEnvelope envelope) =>
        Check.NotNull(envelope, nameof(envelope)).Headers.GetValue<DateTime>(KafkaMessageHeaders.Timestamp);

    /// <summary>
    ///     Gets the offset of the message consumed from Kafka.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <returns>
    ///     The offset of the message.
    /// </returns>
    public static KafkaOffset GetKafkaOffset(this IRawInboundEnvelope envelope) =>
        (KafkaOffset)Check.NotNull(envelope, nameof(envelope)).BrokerMessageIdentifier;
}
