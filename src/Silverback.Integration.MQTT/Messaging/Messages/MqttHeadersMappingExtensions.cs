// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using MQTTnet.Packets;
using Silverback.Util;

namespace Silverback.Messaging.Messages
{
    internal static class MqttHeadersMappingExtensions
    {
        public static List<MqttUserProperty> ToUserProperties(this IReadOnlyCollection<MessageHeader> headers)
        {
            var userProperties = new List<MqttUserProperty>(headers.Count);
            headers.ForEach(header => userProperties.Add(new MqttUserProperty(header.Name, header.Value)));
            return userProperties;
        }

        public static IReadOnlyCollection<MessageHeader> ToSilverbackHeaders(
            this List<MqttUserProperty> userProperties)
        {
            var headers = new List<MessageHeader>(userProperties.Count);
            userProperties.ForEach(
                userProperty =>
                    headers.Add(new MessageHeader(userProperty.Name, userProperty.Value)));
            return headers;
        }
    }
}
