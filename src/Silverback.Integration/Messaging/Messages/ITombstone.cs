// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages;

/// <summary>
///     A tombstone message (a message with null body).
/// </summary>
public interface ITombstone
{
    /// <summary>
    ///     Gets the message identifier.
    /// </summary>
    string? MessageId { get; }
}
