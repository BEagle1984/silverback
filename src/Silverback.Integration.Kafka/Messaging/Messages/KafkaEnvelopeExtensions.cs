// Copyright (c) 2025 Sergio Aquilini
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
        Check.NotNull(envelope, nameof(envelope)).Headers.GetValue(KafkaMessageHeaders.MessageKey);

    /// <summary>
    ///     Sets the key of the message to be produced to Kafka.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <param name="key">
    ///     The Kafka message key.
    /// </param>
    /// <returns>
    ///     The <see cref="IOutboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    public static IOutboundEnvelope SetKafkaKey(this IOutboundEnvelope envelope, string key)
    {
        Check.NotNull(envelope, nameof(envelope));
        envelope.AddOrReplaceHeader(KafkaMessageHeaders.MessageKey, key);
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

    /// <summary>
    ///     Gets the destination topic.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <returns>
    ///     The destination topic.
    /// </returns>
    public static string? GetKafkaDestinationTopic(this IOutboundEnvelope envelope) =>
        Check.NotNull(envelope, nameof(envelope)).Headers.GetValue(KafkaMessageHeaders.DestinationTopic);

    /// <summary>
    ///     Gets destination partition.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <returns>
    ///     The destination partition.
    /// </returns>
    public static int? GetKafkaDestinationPartition(this IOutboundEnvelope envelope) =>
        Check.NotNull(envelope, nameof(envelope)).Headers.GetValue<int>(KafkaMessageHeaders.DestinationPartition);

    /// <summary>
    ///     Sets the destination topic.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <param name="destinationTopic">
    ///     The destination topic.
    /// </param>
    /// <param name="destinationPartition">
    ///     The destination partition.
    /// </param>
    /// <returns>
    ///     The <see cref="IOutboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    public static IOutboundEnvelope SetKafkaDestinationTopic(this IOutboundEnvelope envelope, string destinationTopic, int? destinationPartition = null)
    {
        Check.NotNull(envelope, nameof(envelope));
        envelope.Headers.AddOrReplace(KafkaMessageHeaders.DestinationTopic, destinationTopic);

        if (destinationPartition != null)
            envelope.SetKafkaDestinationPartition(destinationPartition.Value);

        return envelope;
    }

    /// <summary>
    ///     Sets the destination partition.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <param name="destinationPartition">
    ///     The destination partition.
    /// </param>
    /// <returns>
    ///     The <see cref="IOutboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    public static IOutboundEnvelope SetKafkaDestinationPartition(this IOutboundEnvelope envelope, int destinationPartition)
    {
        Check.NotNull(envelope, nameof(envelope));
        envelope.Headers.AddOrReplace(KafkaMessageHeaders.DestinationPartition, destinationPartition);
        return envelope;
    }
}
