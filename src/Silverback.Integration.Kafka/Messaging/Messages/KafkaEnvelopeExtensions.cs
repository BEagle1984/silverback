// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.Text;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Subscribers.ArgumentResolvers;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Adds some convenience extension methods to the envelope interfaces.
/// </summary>
// TODO: Test everything
public static class KafkaEnvelopeExtensions
{
    public static IKafkaInboundEnvelope AsKafkaEnvelope(this IInboundEnvelope envelope) =>
        Check.IsOfType<IKafkaInboundEnvelope>(envelope, nameof(envelope));


    public static IKafkaOutboundEnvelope AsKafkaEnvelope(this IOutboundEnvelope envelope) =>
        Check.IsOfType<IKafkaOutboundEnvelope>(envelope, nameof(envelope));


    /// <summary>
    ///     Gets the key of the message consumed from Kafka.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <returns>
    ///     The Kafka message key.
    /// </returns>
    public static string? GetKafkaKey(this IInboundEnvelope envelope) =>
        GetKafkaKey<string>(envelope);

    /// <summary>
    ///     Gets the key of the message consumed from Kafka.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <returns>
    ///     The Kafka message key.
    /// </returns>
    public static string? GetKafkaKey(this IOutboundEnvelope envelope) =>
        GetKafkaKey<string>(envelope);

    /// <summary>
    ///     Gets the key of the message consumed from Kafka.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <returns>
    ///     The Kafka message key.
    /// </returns>
    public static TKey? GetKafkaKey<TKey>(this IInboundEnvelope envelope) =>
        (TKey?)Convert.ChangeType(Check.NotNull(envelope, nameof(envelope)).AsKafkaEnvelope().Key, typeof(TKey), CultureInfo.InvariantCulture);

    /// <summary>
    ///     Gets the key of the message produced to Kafka.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <returns>
    ///     The Kafka message key.
    /// </returns>
    public static TKey? GetKafkaKey<TKey>(this IOutboundEnvelope envelope) =>
        (TKey?)Convert.ChangeType(Check.NotNull(envelope, nameof(envelope)).AsKafkaEnvelope().Key, typeof(TKey), CultureInfo.InvariantCulture);

    /// <summary>
    ///     Gets the raw key of the message consumed from Kafka.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <returns>
    ///     The Kafka message key.
    /// </returns>
    public static byte[]? GetKafkaRawKey(this IInboundEnvelope envelope) =>
        Check.NotNull(envelope, nameof(envelope)).AsKafkaEnvelope().RawKey;

    /// <summary>
    ///     Gets the raw key of the message produced to Kafka.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <returns>
    ///     The Kafka message key.
    /// </returns>
    public static byte[]? GetKafkaRawKey(this IOutboundEnvelope envelope) =>
        Check.NotNull(envelope, nameof(envelope)).AsKafkaEnvelope().RawKey;

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
    public static IOutboundEnvelope SetKafkaKey<TKey>(this IOutboundEnvelope envelope, TKey key) =>
        Check.NotNull(envelope, nameof(envelope)).AsKafkaEnvelope().SetKey(key);
    // TODO: Improve exception message when the key type mismatch

    /// <summary>
    ///     Sets the raw key of the message to be produced to Kafka.
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
    public static IOutboundEnvelope SetKafkaRawKey(this IOutboundEnvelope envelope, byte[]? key) =>
        Check.NotNull(envelope, nameof(envelope)).AsKafkaEnvelope().SetRawKey(key);
    /// <summary>
    ///     Sets the raw key of the message to be produced to Kafka from the provided UTF8 string.
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
    public static IOutboundEnvelope SetKafkaRawKey(this IOutboundEnvelope envelope, string? key) =>
        Check.NotNull(envelope, nameof(envelope)).AsKafkaEnvelope().SetRawKey(key?.ToUtf8Bytes());

    /// <summary>
    ///     Gets the timestamp of the message consumed from Kafka.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <returns>
    ///     The timestamp of the message.
    /// </returns>
    public static DateTime GetKafkaTimestamp(this IInboundEnvelope envelope) =>
        Check.NotNull(envelope, nameof(envelope)).AsKafkaEnvelope().Timestamp;

    /// <summary>
    ///     Gets the offset of the message consumed from Kafka.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <returns>
    ///     The offset of the message.
    /// </returns>
    public static KafkaOffset GetKafkaOffset(this IInboundEnvelope envelope) =>
        Check.NotNull(envelope, nameof(envelope)).AsKafkaEnvelope().Offset;

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
        Check.NotNull(envelope, nameof(envelope)).AsKafkaEnvelope().DynamicDestinationTopic;

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
        Check.NotNull(envelope, nameof(envelope)).AsKafkaEnvelope().DynamicDestinationPartition;

    /// <summary>
    ///     Sets the destination topic.
    /// </summary>
    /// <param name="envelope">
    ///     The envelope containing the message.
    /// </param>
    /// <param name="topic">
    ///     The destination topic.
    /// </param>
    /// <param name="partition">
    ///     The destination partition.
    /// </param>
    /// <returns>
    ///     The <see cref="IOutboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    public static IOutboundEnvelope SetKafkaDestinationTopic(this IOutboundEnvelope envelope, string topic, int? partition = null) =>
        Check.NotNull(envelope, nameof(envelope)).AsKafkaEnvelope().SetDestinationTopic(topic, partition);

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
    public static IOutboundEnvelope SetKafkaDestinationPartition(this IOutboundEnvelope envelope, int destinationPartition) =>
        Check.NotNull(envelope, nameof(envelope)).AsKafkaEnvelope().SetDestinationPartition(destinationPartition);
}
