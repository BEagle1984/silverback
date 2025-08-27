// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages;

/// <inheritdoc cref="IInboundEnvelope" />
/// <typeparam name="TMessage">
///     The type of the consumed message.
/// </typeparam>
public interface IInboundEnvelope<out TMessage> : IInboundEnvelope, IEnvelope<TMessage>
{
    /// <summary>
    ///     Gets the deserialized message body.
    /// </summary>
    new TMessage? Message { get; }
}
