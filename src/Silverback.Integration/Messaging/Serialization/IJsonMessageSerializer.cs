// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Text.Json;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Serializes and deserializes the messages in JSON format.
/// </summary>
internal interface IJsonMessageSerializer : IMessageSerializer
{
    /// <summary>
    ///     Gets or sets the options to be passed to the <see cref="JsonSerializer" />.
    /// </summary>
    JsonSerializerOptions Options { get; set; }
}
