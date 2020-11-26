// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using MQTTnet.Client.Options;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Comparers
{
    internal class MqttClientWebSocketOptionsEqualityComparer : IEqualityComparer<MqttClientWebSocketOptions>
    {
        private static readonly CollectionEqualityComparer<KeyValuePair<string, string>> RequestHeadersComparer = new();

        private static readonly MqttClientWebSocketProxyOptionsEqualityComparer ProxyOptionsEqualityComparer =
            MqttClientWebSocketProxyOptionsEqualityComparer.Instance;

        private static readonly MqttClientTlsOptionsEqualityComparer TlsOptionsEqualityComparer =
            MqttClientTlsOptionsEqualityComparer.Instance;

        public static MqttClientWebSocketOptionsEqualityComparer Instance { get; } = new();

        public bool Equals(MqttClientWebSocketOptions? x, MqttClientWebSocketOptions? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (ReferenceEquals(x, null))
                return false;
            if (ReferenceEquals(y, null))
                return false;
            if (x.GetType() != y.GetType())
                return false;

            return x.Uri == y.Uri &&
                   RequestHeadersComparer.Equals(x.RequestHeaders, y.RequestHeaders) &&
                   CollectionEqualityComparer.String.Equals(x.SubProtocols, y.SubProtocols) &&
                   Equals(x.CookieContainer, y.CookieContainer) &&
                   ProxyOptionsEqualityComparer.Equals(x.ProxyOptions, y.ProxyOptions) &&
                   TlsOptionsEqualityComparer.Equals(x.TlsOptions, y.TlsOptions);
        }

        public int GetHashCode(MqttClientWebSocketOptions obj) => HashCode.Combine(
            obj.Uri,
            obj.RequestHeaders,
            obj.SubProtocols,
            obj.CookieContainer,
            obj.ProxyOptions,
            obj.TlsOptions);
    }
}
