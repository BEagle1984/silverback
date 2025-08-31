// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages;

/// <inheritdoc cref="IMqttOutboundEnvelope" />
/// <typeparam name="TCorrelationData">
///     The type of the correlation data.
/// </typeparam>
public interface IMqttOutboundEnvelope<TCorrelationData> : IMqttOutboundEnvelope
{
    /// <summary>
    ///     Gets the correlation data.
    /// </summary>
    new TCorrelationData? CorrelationData { get; }

    /// <summary>
    ///     Sets the correlation data.
    /// </summary>
    /// <param name="correlationData">
    ///     The correlation data.
    /// </param>
    /// <returns>
    ///     The <see cref="IMqttOutboundEnvelope" /> so that additional calls can be chained.
    /// </returns>
    IMqttOutboundEnvelope SetCorrelationData(TCorrelationData? correlationData);
}
