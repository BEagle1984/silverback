// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Wraps the message to be produced to MQTT.
/// </summary>
public interface IMqttOutboundEnvelope : IOutboundEnvelope
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

    /// <summary>
    ///     Gets the destination topic for the message. Used when dynamic routing is enabled.
    /// </summary>
    string? DynamicDestinationTopic { get; }

    /// <summary>
    ///     Sets the serialized correlation data.
    /// </summary>
    /// <param name="rawCorrelationData">
    ///     The serialized correlation data.
    /// </param>
    /// <returns>
    ///     The <see cref="IMqttOutboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    IMqttOutboundEnvelope SetRawCorrelationData(byte[]? rawCorrelationData);

    /// <summary>
    ///     Sets the response topic.
    /// </summary>
    /// <param name="topic">
    ///     The response topic.
    /// </param>
    /// <returns>
    ///     The <see cref="IMqttOutboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    IMqttOutboundEnvelope SetResponseTopic(string? topic);

    /// <summary>
    ///     Sets the destination topic for the message. Dynamic routing must be enabled.
    /// </summary>
    /// <param name="topic">
    ///     The destination topic.
    /// </param>
    /// <returns>
    ///     The <see cref="IMqttOutboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    IMqttOutboundEnvelope SetDestinationTopic(string? topic);
}
