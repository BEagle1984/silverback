// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using MQTTnet.Packets;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

internal static class MqttHeadersMappingExtensions
{
    public static List<MqttUserProperty> ToUserProperties(this IReadOnlyCollection<MessageHeader> headers)
    {
        List<MqttUserProperty> userProperties = new(headers.Count);
        headers.Where(header => header.Value != null).ForEach(header => userProperties.Add(new MqttUserProperty(header.Name, header.Value)));
        return userProperties;
    }

    public static IReadOnlyCollection<MessageHeader> ToSilverbackHeaders(this List<MqttUserProperty> userProperties)
    {
        List<MessageHeader> headers = new(userProperties.Count);
        userProperties.ForEach(userProperty => headers.Add(new MessageHeader(userProperty.Name, userProperty.Value)));
        return headers;
    }
}
