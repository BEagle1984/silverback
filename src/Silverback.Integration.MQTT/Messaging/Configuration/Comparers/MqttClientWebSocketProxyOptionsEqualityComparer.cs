// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using MQTTnet.Client.Options;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Comparers
{
    internal class MqttClientWebSocketProxyOptionsEqualityComparer : IEqualityComparer<MqttClientWebSocketProxyOptions>
    {
        public static MqttClientWebSocketProxyOptionsEqualityComparer Instance { get; } = new();

        public bool Equals(MqttClientWebSocketProxyOptions? x, MqttClientWebSocketProxyOptions? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (ReferenceEquals(x, null))
                return false;
            if (ReferenceEquals(y, null))
                return false;
            if (x.GetType() != y.GetType())
                return false;

            return x.Address == y.Address &&
                   x.Username == y.Username &&
                   x.Password == y.Password &&
                   x.Domain == y.Domain &&
                   x.BypassOnLocal == y.BypassOnLocal &&
                   CollectionEqualityComparer.String.Equals(x.BypassList, y.BypassList);
        }

        public int GetHashCode(MqttClientWebSocketProxyOptions obj) =>
            HashCode.Combine(
                obj.Address,
                obj.Username,
                obj.Password,
                obj.Domain,
                obj.BypassOnLocal,
                obj.BypassList);
    }
}
