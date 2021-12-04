// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Newtonsoft.Json;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Serializes and deserializes the messages in JSON format.
/// </summary>
internal interface INewtonsoftJsonMessageSerializer : IMessageSerializer
{
    /// <summary>
    ///     Gets or sets the message encoding. The default is UTF8.
    /// </summary>
    MessageEncoding Encoding { get; set; }

    /// <summary>
    ///     Gets or sets the settings to be applied to the Json.NET serializer.
    /// </summary>
    JsonSerializerSettings Settings { get; set; }
}
