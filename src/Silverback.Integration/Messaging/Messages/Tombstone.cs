// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Messages;

/// <summary>
///     A tombstone message (a message with null body).
/// </summary>
public class Tombstone : ITombstone
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Tombstone" /> class.
    /// </summary>
    /// <param name="messageId">
    ///     The message identifier.
    /// </param>
    public Tombstone(string? messageId)
    {
        MessageId = messageId;
    }

    /// <inheritdoc cref="ITombstone.MessageId" />
    [Header(DefaultMessageHeaders.MessageId)]
    public string? MessageId { get; }

    /// <inheritdoc cref="ITombstone.MessageType" />
    public virtual Type MessageType { get; } = typeof(object);
}
