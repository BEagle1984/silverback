// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Wraps the message consumed from Kafka.
/// </summary>
public interface IKafkaInboundEnvelope : IInboundEnvelope
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
    ///     Gets the message offset.
    /// </summary>
    KafkaOffset Offset { get; }

    /// <summary>
    ///     Gets the message timestamp.
    /// </summary>
    DateTime Timestamp { get; }
}
