// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Wraps the message to be produced to Kafka.
/// </summary>
public interface IKafkaOutboundEnvelope : IOutboundEnvelope
{
    /// <summary>
    ///     Gets the message key.
    /// </summary>
    object? Key { get; }

    /// <summary>
    ///     Gets the serialized message key.
    /// </summary>
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Reviewed")]
    byte[]? RawKey { get; }

    /// <summary>
    ///     Gets the message offset. The offset is set only after the message has been successfully produced.
    /// </summary>
    KafkaOffset? Offset { get; }

    /// <summary>
    ///     Gets the destination topic for the message. Used when dynamic routing is enabled.
    /// </summary>
    string? DynamicDestinationTopic { get; }

    /// <summary>
    ///     Gets the destination partition for the message. Used when dynamic routing is enabled.
    /// </summary>
    int? DynamicDestinationPartition { get; }

    /// <summary>
    ///     Sets the message key.
    /// </summary>
    /// <param name="key">
    ///     The message key.
    /// </param>
    /// <returns>
    ///     The <see cref="IKafkaOutboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    IKafkaOutboundEnvelope SetKey(object? key);

    /// <summary>
    ///     Sets the message key.
    /// </summary>
    /// <param name="rawKey">
    ///     The serialized message key.
    /// </param>
    /// <returns>
    ///     The <see cref="IKafkaOutboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    IKafkaOutboundEnvelope SetRawKey(byte[]? rawKey);

    /// <summary>
    ///     Sets the destination topic and partition for the message. Dynamic routing must be enabled.
    /// </summary>
    /// <param name="topic">
    ///     The destination topic.
    /// </param>
    /// <param name="partition">
    ///     The destination partition.
    /// </param>
    /// <returns>
    ///     The <see cref="IKafkaOutboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    IKafkaOutboundEnvelope SetDestinationTopic(string? topic, int? partition = null);

    /// <summary>
    ///     Sets the destination partition for the message. Dynamic routing must be enabled.
    /// </summary>
    /// <param name="partition">
    ///     The destination partition.
    /// </param>
    /// <returns>
    ///     The <see cref="IKafkaOutboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    IKafkaOutboundEnvelope SetDestinationPartition(int? partition);
}
