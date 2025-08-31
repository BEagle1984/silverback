// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages;

/// <inheritdoc cref="IMqttOutboundEnvelope" />
/// <typeparam name="TMessage">
///     The type of the wrapped message.
/// </typeparam>
public interface IMqttOutboundEnvelope<out TMessage> : IMqttOutboundEnvelope, IOutboundEnvelope<TMessage>;
