// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Messages;

/// <summary>
///     A tombstone message (a message with null body).
/// </summary>
/// <typeparam name="TMessage">
///     The type of the message that was expected.
/// </typeparam>
[SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Used for routing")]
public class Tombstone<TMessage> : Tombstone, ITombstone<TMessage>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Tombstone{TMessage}" /> class.
    /// </summary>
    /// <param name="messageKey">
    ///     The message identifier.
    /// </param>
    public Tombstone(string messageKey)
        : base(messageKey)
    {
    }

    /// <inheritdoc cref="ITombstone.MessageType" />
    public override Type MessageType { get; } = typeof(TMessage);
}
