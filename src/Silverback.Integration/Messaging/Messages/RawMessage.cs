// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using System.Threading.Tasks;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Wraps a raw message <see cref="Stream" />.
/// </summary>
public class RawMessage
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="RawMessage" /> class.
    /// </summary>
    /// <param name="content">
    ///     The message content.
    /// </param>
    public RawMessage(Stream? content)
    {
        Content = content;
    }

    /// <summary>
    ///     Gets the message content.
    /// </summary>
    public Stream? Content { get; }

    /// <summary>
    ///     Implicitly converts a <see cref="Stream" /> to a <see cref="RawMessage" />.
    /// </summary>
    /// <param name="content">
    ///     The message content.
    /// </param>
    /// <returns>
    ///     A <see cref="RawMessage" /> wrapping the specified content.
    /// </returns>
    public static implicit operator RawMessage(Stream? content) => new(content);

    /// <summary>
    ///     Implicitly converts a <see cref="byte" /> array to a <see cref="RawMessage" />.
    /// </summary>
    /// <param name="content">
    ///     The message content.
    /// </param>
    /// <returns>
    ///     A <see cref="RawMessage" /> wrapping the specified content.
    /// </returns>
    public static implicit operator RawMessage(byte[]? content) => new(content == null ? null : new MemoryStream(content));

    /// <summary>
    ///     Implicitly converts a <see cref="RawMessage" /> to a <see cref="Stream" />.
    /// </summary>
    /// <param name="message">
    ///     The <see cref="RawMessage" />.
    /// </param>
    /// <returns>
    ///     The message content.
    /// </returns>
    public static implicit operator Stream?(RawMessage? message) => message?.Content;

    /// <summary>
    ///     Implicitly converts a <see cref="RawMessage" /> to a <see cref="byte" /> array.
    /// </summary>
    /// <param name="message">
    ///     The <see cref="RawMessage" />.
    /// </param>
    /// <returns>
    ///     The message content.
    /// </returns>
    public static implicit operator byte[]?(RawMessage? message) => message?.Content.ReadAll();

    /// <summary>
    ///     Creates a new <see cref="RawMessage" /> instance from the specified content.
    /// </summary>
    /// <param name="content">
    ///     The message content.
    /// </param>
    /// <returns>
    ///     A <see cref="RawMessage" /> wrapping the specified content.
    /// </returns>
    public static RawMessage FromStream(Stream? content) => new(content);

    /// <summary>
    ///     Creates a new <see cref="RawMessage" /> instance from the specified content.
    /// </summary>
    /// <param name="content">
    ///     The message content.
    /// </param>
    /// <returns>
    ///     A <see cref="RawMessage" /> wrapping the specified content.
    /// </returns>
    public static RawMessage FromByteArray(byte[]? content) => new(content == null ? null : new MemoryStream(content));

    /// <summary>
    ///     Returns the message content.
    /// </summary>
    /// <returns>
    ///     The message content.
    /// </returns>
    public Stream? ToStream() => Content;

    /// <summary>
    ///     Returns the message content.
    /// </summary>
    /// <returns>
    ///     The message content.
    /// </returns>
    public byte[]? ToByteArray() => Content.ReadAll();

    /// <summary>
    ///     Returns the message content.
    /// </summary>
    /// <returns>
    ///     The message content.
    /// </returns>
    public ValueTask<byte[]?> ToByteArrayAsync() => Content.ReadAllAsync();
}
