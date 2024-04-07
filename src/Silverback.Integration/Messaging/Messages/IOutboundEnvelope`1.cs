// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages;

/// <inheritdoc cref="IOutboundEnvelope" />
public interface IOutboundEnvelope<out TMessage> : IOutboundEnvelope, IEnvelope<TMessage>
{
    /// <summary>
    ///     Gets the deserialized message body.
    /// </summary>
    new TMessage? Message { get; }
}
