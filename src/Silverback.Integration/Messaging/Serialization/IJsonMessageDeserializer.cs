// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Text.Json;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Deserializes the messages from JSON.
/// </summary>
internal interface IJsonMessageDeserializer : IMessageDeserializer
{
    /// <summary>
    ///     Gets or sets the options to be passed to the <see cref="JsonSerializer" />.
    /// </summary>
    JsonSerializerOptions? Options { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating how the deserialize according to the message type header.
    /// </summary>
    JsonMessageDeserializerTypeHeaderBehavior TypeHeaderBehavior { get; set; }
}
