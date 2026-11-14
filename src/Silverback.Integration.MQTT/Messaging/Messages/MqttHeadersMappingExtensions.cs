// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTTnet.Packets;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

internal static class MqttHeadersMappingExtensions
{
    public static List<MqttUserProperty> ToUserProperties(this IReadOnlyCollection<MessageHeader> headers, bool isChunkingEnabled = false)
    {
        List<MqttUserProperty> userProperties = new(headers.Count + 1);
        headers.Where(header => header.Value != null &&
                                !header.Name.StartsWith(DefaultMessageHeaders.InternalHeadersPrefix, StringComparison.Ordinal))
            .ForEach(header => userProperties.Add(CreateUserProperty(header.Name, header.Value)));

        // The message key correlating the chunks (set when chunking) is an internal header: send it as the chunked message identifier.
        // The chunk headers can't be used to recognize the chunks, since they can be renamed (see WithCustomHeaderName).
        if (isChunkingEnabled &&
            headers.All(header => header.Name != MqttMessageHeaders.ChunkMessageId) &&
            headers.FirstOrDefault(header => header.Name == DefaultMessageHeaders.MessageKey)?.Value is { } messageKey)
        {
            userProperties.Add(CreateUserProperty(MqttMessageHeaders.ChunkMessageId, messageKey));
        }

        return userProperties;
    }

    public static IReadOnlyCollection<MessageHeader> ToSilverbackHeaders(this List<MqttUserProperty> userProperties)
    {
        List<MessageHeader> headers = new(userProperties.Count);
        userProperties.ForEach(userProperty => headers.Add(new MessageHeader(userProperty.Name, userProperty.ReadValueAsString())));
        return headers;
    }

    private static MqttUserProperty CreateUserProperty(string name, string? value) =>
        new(name, new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(value ?? string.Empty)));
}
