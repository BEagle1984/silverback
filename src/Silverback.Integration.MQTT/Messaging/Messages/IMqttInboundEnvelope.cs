// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Wraps the message consumed from Mqtt.
/// </summary>
public interface IMqttInboundEnvelope : IInboundEnvelope
{
    /// <summary>
    ///     Gets the correlation data.
    /// </summary>
    object? CorrelationData { get; }

    /// <summary>
    ///     Gets the serialized correlation data.
    /// </summary>
    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Reviewed")]
    byte[]? RawCorrelationData { get; }

    /// <summary>
    ///     Gets the response topic.
    /// </summary>
    string? ResponseTopic { get; }
}
