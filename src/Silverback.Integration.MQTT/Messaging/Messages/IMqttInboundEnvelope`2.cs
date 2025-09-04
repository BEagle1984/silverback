// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages;

/// <inheritdoc cref="IMqttInboundEnvelope" />
/// <typeparam name="TMessage">
///     The type of the wrapped message.
/// </typeparam>
/// <typeparam name="TCorrelationData">
///     The type of the correlation data.
/// </typeparam>
public interface IMqttInboundEnvelope<out TMessage, out TCorrelationData> : IMqttInboundEnvelope<TMessage>
{
    /// <summary>
    ///     Gets the correlation data.
    /// </summary>
    new TCorrelationData? CorrelationData { get; }
}
