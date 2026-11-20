// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Text;
using MQTTnet.Packets;

namespace Silverback.Messaging.Messages;

internal static class MqttHeadersMappingExtensions
{
    public static List<MqttUserProperty> ToUserProperties(this IReadOnlyCollection<MessageHeader> headers, bool isChunkingEnabled = false)
    {
        List<MqttUserProperty> userProperties = new(headers.Count + 1);
        bool hasChunkMessageId = false;
        MessageHeader? messageKeyHeader = null;

        foreach (MessageHeader header in headers)
        {
            if (header.Name == MqttMessageHeaders.ChunkMessageId)
                hasChunkMessageId = true;

            if (messageKeyHeader == null && header.Name == DefaultMessageHeaders.MessageKey)
                messageKeyHeader = header;

            if (header.Value != null && !header.Name.StartsWith(DefaultMessageHeaders.InternalHeadersPrefix, StringComparison.Ordinal))
                userProperties.Add(CreateUserProperty(header.Name, header.Value));
        }

        // The message key correlating the chunks (set when chunking) is an internal header: send it as the chunked message identifier.
        // The chunk headers can't be used to recognize the chunks, since they can be renamed (see WithCustomHeaderName).
        if (isChunkingEnabled && !hasChunkMessageId && messageKeyHeader?.Value is { } messageKey)
            userProperties.Add(CreateUserProperty(MqttMessageHeaders.ChunkMessageId, messageKey));

        return userProperties;
    }

    // The additional capacity reserves room for the headers that the caller is about to add
    public static MessageHeaderCollection ToSilverbackHeaders(this List<MqttUserProperty> userProperties, int additionalCapacity = 0)
    {
        MessageHeaderCollection headers = new(userProperties.Count + additionalCapacity);

        foreach (MqttUserProperty userProperty in userProperties)
        {
            headers.Add(new MessageHeader(userProperty.Name, userProperty.ReadValueAsString()));
        }

        return headers;
    }

    private static MqttUserProperty CreateUserProperty(string name, string? value) =>
        new(name, new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(value ?? "")));
}
