// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Newtonsoft.Json;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Serializes and deserializes the messages in JSON format.
/// </summary>
internal interface INewtonsoftJsonMessageSerializer : IMessageSerializer
{
    /// <summary>
    ///     Gets the message encoding. The default is UTF8.
    /// </summary>
    MessageEncoding Encoding { get; }

    /// <summary>
    ///     Gets the settings to be applied to the Json.NET serializer.
    /// </summary>
    JsonSerializerSettings Settings { get; }

    /// <summary>
    ///     Gets or sets a value indicating whether the message type header (see <see cref="DefaultMessageHeaders.MessageType"/>) must be set.
    ///     This is necessary when sending multiple message type through the same endpoint, to allow Silverback to automatically figure out
    ///     the correct type to deserialize into.
    /// </summary>
    public bool MustSetTypeHeader { get; set; }
}
