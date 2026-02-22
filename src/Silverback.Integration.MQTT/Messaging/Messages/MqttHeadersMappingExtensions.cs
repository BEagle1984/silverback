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
    public static List<MqttUserProperty> ToUserProperties(this IReadOnlyCollection<MessageHeader> headers)
    {
        List<MqttUserProperty> userProperties = new(headers.Count);
        headers.Where(header => header.Value != null &&
                                !header.Name.StartsWith(DefaultMessageHeaders.InternalHeadersPrefix, StringComparison.Ordinal))
            .ForEach(header => userProperties.Add(
                new MqttUserProperty(
                    header.Name,
                    new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(header.Value ?? string.Empty)))));
        return userProperties;
    }

    public static IReadOnlyCollection<MessageHeader> ToSilverbackHeaders(this List<MqttUserProperty> userProperties)
    {
        List<MessageHeader> headers = new(userProperties.Count);
        userProperties.ForEach(userProperty => headers.Add(new MessageHeader(userProperty.Name, userProperty.ReadValueAsString())));
        return headers;
    }
}
