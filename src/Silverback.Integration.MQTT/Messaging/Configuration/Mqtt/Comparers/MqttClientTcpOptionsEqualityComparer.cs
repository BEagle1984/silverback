// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using MQTTnet.Client.Options;

namespace Silverback.Messaging.Configuration.Mqtt.Comparers
{
    internal sealed class MqttClientTcpOptionsEqualityComparer : IEqualityComparer<MqttClientTcpOptions>
    {
        private static readonly MqttClientTlsOptionsEqualityComparer TlsOptionsEqualityComparer =
            MqttClientTlsOptionsEqualityComparer.Instance;

        public static MqttClientTcpOptionsEqualityComparer Instance { get; } = new();

        public bool Equals(MqttClientTcpOptions? x, MqttClientTcpOptions? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (ReferenceEquals(x, null))
                return false;
            if (ReferenceEquals(y, null))
                return false;
            if (x.GetType() != y.GetType())
                return false;

            return x.Server == y.Server &&
                   x.Port == y.Port &&
                   x.BufferSize == y.BufferSize &&
                   x.DualMode == y.DualMode &&
                   x.NoDelay == y.NoDelay &&
                   x.AddressFamily == y.AddressFamily &&
                   TlsOptionsEqualityComparer.Equals(x.TlsOptions, y.TlsOptions);
        }

        public int GetHashCode(MqttClientTcpOptions obj) => HashCode.Combine(
            obj.Server,
            obj.Port,
            obj.BufferSize,
            obj.DualMode,
            obj.NoDelay,
            (int)obj.AddressFamily,
            obj.TlsOptions);
    }
}
