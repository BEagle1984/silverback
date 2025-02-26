// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Wraps a raw message <see cref="Stream" />.
/// </summary>
/// <typeparam name="T">
///     The type discriminator.
/// </typeparam>
public sealed class RawMessage<T>(Stream? content) : RawMessage(content)
{
    /// <summary>
    ///     Implicitly converts a <see cref="byte" /> array to a <see cref="RawMessage{T}" />.
    /// </summary>
    /// <param name="content">
    ///     The message content.
    /// </param>
    /// <returns>
    ///     A <see cref="RawMessage{T}" /> wrapping the specified content.
    /// </returns>
    public static implicit operator RawMessage<T>(byte[]? content) => new(content == null ? null : new MemoryStream(content));

    /// <summary>
    ///     Implicitly converts a <see cref="Stream" />s to a <see cref="RawMessage{T}" />.
    /// </summary>
    /// <param name="content">
    ///     The message content.
    /// </param>
    /// <returns>
    ///     A <see cref="RawMessage{T}" /> wrapping the specified content.
    /// </returns>
    public static implicit operator RawMessage<T>(Stream? content) => new(content);

    /// <summary>
    ///     Creates a new <see cref="RawMessage{T}" /> instance from the specified content.
    /// </summary>
    /// <param name="content">
    ///     The message content.
    /// </param>
    /// <returns>
    ///     A <see cref="RawMessage" /> wrapping the specified content.
    /// </returns>
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Convenience method")]
    public static new RawMessage<T> FromStream(Stream? content) => new(content);

    /// <summary>
    ///     Creates a new <see cref="RawMessage{T}" /> instance from the specified content.
    /// </summary>
    /// <param name="content">
    ///     The message content.
    /// </param>
    /// <returns>
    ///     A <see cref="RawMessage" /> wrapping the specified content.
    /// </returns>
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Convenience method")]
    public static new RawMessage<T> FromByteArray(byte[]? content) => new(content == null ? null : new MemoryStream(content));
}
