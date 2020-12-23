// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using MQTTnet.Client.Options;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Mqtt.Comparers
{
    internal class MqttClientCredentialsEqualityComparer : IEqualityComparer<IMqttClientCredentials>
    {
        public static MqttClientCredentialsEqualityComparer Instance { get; } = new();

        public bool Equals(IMqttClientCredentials? x, IMqttClientCredentials? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (ReferenceEquals(x, null))
                return false;
            if (ReferenceEquals(y, null))
                return false;
            if (x.GetType() != y.GetType())
                return false;

            return x.Username == y.Username &&
                   CollectionEqualityComparer.Byte.Equals(x.Password, y.Password);
        }

        public int GetHashCode(IMqttClientCredentials obj) =>
            HashCode.Combine(obj.Username, obj.Password);
    }
}
