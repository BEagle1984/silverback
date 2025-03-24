// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Wraps a message when it's being transferred over a message broker.
/// </summary>
public interface IEnvelope
{
    /// <summary>
    ///     Gets the message.
    /// </summary>
    object? Message { get; }

    /// <summary>
    ///     Gets the type of the message.
    /// </summary>
    Type MessageType { get; }
}
