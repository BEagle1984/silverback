﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using MQTTnet.Client.Options;

namespace Silverback.Messaging.Configuration.Mqtt.Comparers
{
    internal sealed class MqttClientChannelOptionsEqualityComparer : IEqualityComparer<IMqttClientChannelOptions>
    {
        private static readonly MqttClientTcpOptionsEqualityComparer TcpOptionsEqualityComparer =
            MqttClientTcpOptionsEqualityComparer.Instance;

        private static readonly MqttClientWebSocketOptionsEqualityComparer WebSocketOptionsEqualityComparer =
            MqttClientWebSocketOptionsEqualityComparer.Instance;

        public static MqttClientChannelOptionsEqualityComparer Instance { get; } = new();

        public bool Equals(IMqttClientChannelOptions? x, IMqttClientChannelOptions? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (ReferenceEquals(x, null))
                return false;
            if (ReferenceEquals(y, null))
                return false;
            if (x.GetType() != y.GetType())
                return false;

            if (x is MqttClientTcpOptions xTcp)
                return TcpOptionsEqualityComparer.Equals(xTcp, (MqttClientTcpOptions)y);
            if (x is MqttClientWebSocketOptions xWebSocket)
                return WebSocketOptionsEqualityComparer.Equals(xWebSocket, (MqttClientWebSocketOptions)y);

            return false;
        }

        public int GetHashCode(IMqttClientChannelOptions obj) =>
            obj switch
            {
                MqttClientTcpOptions objTcp => TcpOptionsEqualityComparer.GetHashCode(objTcp),
                MqttClientWebSocketOptions objWebSocket => WebSocketOptionsEqualityComparer.GetHashCode(objWebSocket),
                _ => HashCode.Combine(obj)
            };
    }
}
