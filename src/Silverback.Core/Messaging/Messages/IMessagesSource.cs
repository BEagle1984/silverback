// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Exposes the methods to retrieve a list of messages associated with the object implementing this
///     interface.
/// </summary>
/// <remarks>
///     Used to implement the domain entities and automatically publish their events when the entity is
///     saved to the underlying database.
/// </remarks>
public interface IMessagesSource
{
    /// <summary>
    ///     Gets the messages to be published.
    /// </summary>
    /// <returns>
    ///     The message objects.
    /// </returns>
    IEnumerable<object>? GetMessages();

    /// <summary>
    ///     Called after the messages have been successfully published (and processed) to clear the messages
    ///     collection.
    /// </summary>
    void ClearMessages();
}
