// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages;

/// <inheritdoc cref="IOutboundEnvelope" />
/// <typeparam name="TMessage">
///     The type of the message being produced.
/// </typeparam>
public interface IOutboundEnvelope<out TMessage> : IOutboundEnvelope, IEnvelope<TMessage>
{
    /// <summary>
    ///     Gets the deserialized message body.
    /// </summary>
    new TMessage? Message { get; }
}
