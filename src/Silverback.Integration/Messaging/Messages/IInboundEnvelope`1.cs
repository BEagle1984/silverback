// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages;

/// <inheritdoc cref="IInboundEnvelope" />
/// <typeparam name="TMessage">
///     The type of the message being consumed.
/// </typeparam>
public interface IInboundEnvelope<out TMessage> : IInboundEnvelope, IEnvelope<TMessage>
{
    /// <summary>
    ///     Gets the deserialized message body.
    /// </summary>
    new TMessage? Message { get; }
}
