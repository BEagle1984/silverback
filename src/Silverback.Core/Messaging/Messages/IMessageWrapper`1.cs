// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages;

/// <summary>
///     Represents a wrapped or enriched message.
/// </summary>
/// <typeparam name="T">
///     The type of the message.
/// </typeparam>
public interface IMessageWrapper<out T> : IMessageWrapper
{
    /// <inheritdoc cref="IMessageWrapper.Message" />
    new T? Message { get; }

    /// <inheritdoc cref="IMessageWrapper.Message" />
    object? IMessageWrapper.Message => Message;
}
