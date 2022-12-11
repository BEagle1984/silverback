// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Text.Json.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

/// <summary>
///     An header added to the message being sent over a message broker.
/// </summary>
public readonly record struct MessageHeader
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageHeader" /> class.
    /// </summary>
    /// <param name="name">
    ///     The header name.
    /// </param>
    /// <param name="value">
    ///     The header value.
    /// </param>
    public MessageHeader(string name, object? value)
        : this(name, value?.ToString())
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MessageHeader" /> class.
    /// </summary>
    /// <param name="name">
    ///     The header name.
    /// </param>
    /// <param name="value">
    ///     The header value.
    /// </param>
    [JsonConstructor]
    public MessageHeader(string name, string? value)
    {
        Name = Check.NotNullOrEmpty(name, nameof(name));
        Value = value;
    }

    /// <summary>
    ///     Gets the header name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    ///     Gets the header value.
    /// </summary>
    public string? Value { get; init; }
}
