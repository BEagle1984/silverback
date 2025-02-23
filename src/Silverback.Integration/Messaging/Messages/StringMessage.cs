// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Messages;

/// <summary>
///     The model used with the <see cref="StringMessageSerializer" /> and <see cref="StringMessageDeserializer{TMessage}" />.
/// </summary>
public class StringMessage : IEquatable<StringMessage>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="StringMessage" /> class.
    /// </summary>
    /// <param name="content">
    ///     The message content.
    /// </param>
    public StringMessage(string? content)
    {
        Content = content;
    }

    /// <summary>
    ///     Gets the message content.
    /// </summary>
    public string? Content { get; }

    /// <summary>
    ///     Implicitly converts a <see cref="StringMessage" /> to a <see cref="string" />.
    /// </summary>
    /// <param name="content">
    ///     The message content.
    /// </param>
    /// <returns>
    ///     A <see cref="StringMessage" /> wrapping the specified content.
    /// </returns>
    public static implicit operator StringMessage(string? content) => new(content);

    /// <summary>
    ///     Implicitly converts a <see cref="StringMessage" /> to a <see cref="string" />.
    /// </summary>
    /// <param name="message">
    ///     The <see cref="StringMessage" />.
    /// </param>
    /// <returns>
    ///     The message content.
    /// </returns>
    public static implicit operator string?(StringMessage? message) => message?.Content;

    /// <inheritdoc cref="op_Equality" />
    public static bool operator ==(StringMessage? left, StringMessage? right) => Equals(left, right);

    /// <inheritdoc cref="op_Inequality" />
    public static bool operator !=(StringMessage? left, StringMessage? right) => !Equals(left, right);

    /// <summary>
    ///     Creates a new <see cref="StringMessage" /> instance from the specified content.
    /// </summary>
    /// <param name="content">
    ///     The message content.
    /// </param>
    /// <returns>
    ///     A <see cref="StringMessage" /> wrapping the specified content.
    /// </returns>
    public static StringMessage FromString(string? content) => new(content);

    /// <summary>
    ///     Returns the message content.
    /// </summary>
    /// <returns>
    ///     The message content.
    /// </returns>
    public override string? ToString() => Content;

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(StringMessage? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return GetType() == other.GetType() && Content == other.Content;
    }

    /// <inheritdoc cref="object.Equals(object?)" />
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (obj.GetType() != GetType())
            return false;

        return Equals((StringMessage)obj);
    }

    /// <inheritdoc cref="object.GetHashCode" />
    public override int GetHashCode() => Content?.GetHashCode(StringComparison.Ordinal) ?? 0;
}
