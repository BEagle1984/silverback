// Copyright (c) 2023 Sergio Aquilini
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
    public static string? GetKafkaKey(this IRawInboundEnvelope envelope) =>
        Check.NotNull(envelope, nameof(envelope)).Headers.GetValue(DefaultMessageHeaders.MessageId);

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
