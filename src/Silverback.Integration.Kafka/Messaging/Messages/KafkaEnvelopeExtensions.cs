// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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

    public static IKafkaInboundEnvelope<TMessage> AsKafkaEnvelope<TMessage>(this IInboundEnvelope envelope) =>
        Check.IsOfType<IKafkaInboundEnvelope<TMessage>>(envelope, nameof(envelope));

    public static IKafkaInboundEnvelope<TMessage, TKey> AsKafkaEnvelope<TMessage, TKey>(this IInboundEnvelope envelope) =>
        Check.IsOfType<IKafkaInboundEnvelope<TMessage, TKey>>(envelope, nameof(envelope));

    public static IKafkaOutboundEnvelope AsKafkaEnvelope(this IOutboundEnvelope envelope) =>
        Check.IsOfType<IKafkaOutboundEnvelope>(envelope, nameof(envelope));

    public static IKafkaOutboundEnvelope<TMessage> AsKafkaEnvelope<TMessage>(this IOutboundEnvelope envelope) =>
        Check.IsOfType<IKafkaOutboundEnvelope<TMessage>>(envelope, nameof(envelope));

    public static IKafkaOutboundEnvelope<TMessage, TKey> AsKafkaEnvelope<TMessage, TKey>(this IOutboundEnvelope envelope) =>
        Check.IsOfType<IKafkaOutboundEnvelope<TMessage, TKey>>(envelope, nameof(envelope));

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
        Check.NotNull(envelope, nameof(envelope)).AsKafkaEnvelope<object, TKey>().Key;

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
        Check.NotNull(envelope, nameof(envelope)).AsKafkaEnvelope<object, TKey>().Key;

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
        Check.NotNull(envelope, nameof(envelope)).AsKafkaEnvelope<object, TKey>().SetKey(key);

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
