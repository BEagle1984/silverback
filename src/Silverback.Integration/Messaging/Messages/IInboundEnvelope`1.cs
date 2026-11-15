// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

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

    /// <summary>
    ///     Gets a value indicating whether the message is a tombstone (i.e. it's just a marker to indicate that the message with the same
    ///     key has been deleted). When it is <c>false</c>, the <see cref="Message" /> is not <c>null</c>.
    /// </summary>
    /// <returns>
    ///     A value indicating whether the message is a tombstone.
    /// </returns>
    [MemberNotNullWhen(false, nameof(Message))]
    new bool IsTombstone { get; }
}
