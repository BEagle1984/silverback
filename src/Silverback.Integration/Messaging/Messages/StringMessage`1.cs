// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Wraps a raw message <see cref="string"/>.
/// </summary>
/// <typeparam name="T">
///     The type discriminator.
/// </typeparam>
public sealed class StringMessage<T>(string? content) : StringMessage(content)
{
    /// <summary>
    ///     Implicitly converts a <see cref="string" /> to a <see cref="StringMessage{T}" />.
    /// </summary>
    /// <param name="content">
    ///     The message content.
    /// </param>
    /// <returns>
    ///     A <see cref="StringMessage{T}" /> wrapping the specified content.
    /// </returns>
    public static implicit operator StringMessage<T>(string? content) => new(content);

    /// <summary>
    ///     Creates a new <see cref="StringMessage" /> instance from the specified content.
    /// </summary>
    /// <param name="content">
    ///     The message content.
    /// </param>
    /// <returns>
    ///     A <see cref="StringMessage" /> wrapping the specified content.
    /// </returns>
    [SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Convenience method")]
    public static new StringMessage<T> FromString(string? content) => new(content);
}
