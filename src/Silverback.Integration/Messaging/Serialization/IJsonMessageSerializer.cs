// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Text.Json;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Serializes the messages as JSON.
/// </summary>
internal interface IJsonMessageSerializer : IMessageSerializer
{
    /// <summary>
    ///     Gets or sets the options to be passed to the <see cref="JsonSerializer" />.
    /// </summary>
    JsonSerializerOptions? Options { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the message type header (see <see cref="DefaultMessageHeaders.MessageType"/>) must be set.
    ///     This is necessary when sending multiple message type through the same endpoint, to allow Silverback to automatically figure out
    ///     the correct type to deserialize into.
    /// </summary>
    public bool MustSetTypeHeader { get; set; }
}
