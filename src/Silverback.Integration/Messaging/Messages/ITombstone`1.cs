// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages;

/// <summary>
///     A tombstone message (a message with null body).
/// </summary>
/// <typeparam name="TMessage">
///     The type of the message that was expected.
/// </typeparam>
public interface ITombstone<out TMessage> : ITombstone;
