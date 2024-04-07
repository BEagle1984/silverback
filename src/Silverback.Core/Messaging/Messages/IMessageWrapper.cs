// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages;

/// <summary>
///     Represents a wrapped or enriched message.
/// </summary>
public interface IMessageWrapper
{
    /// <summary>
    ///     Gets the message.
    /// </summary>
    object? Message { get; }
}
