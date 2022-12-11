// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using MQTTnet;
using MQTTnet.Client.Options;
using Silverback.Messaging.Configuration.Mqtt;

namespace Silverback.Messaging.Broker.Mqtt.Mocks;

internal sealed class Subscription : ParsedTopic
{
    private readonly IMqttClientOptions _clientOptions;

    public Subscription(IMqttClientOptions clientOptions, string topic)
        : base(topic)
    {
        _clientOptions = clientOptions;
    }

    public bool IsMatch(MqttApplicationMessage message, IMqttClientOptions clientOptions)
    {
        if (!IsSameBroker(clientOptions))
            return false;

        return Regex?.IsMatch(message.Topic) ?? Topic == message.Topic;
    }

    private bool IsSameBroker(IMqttClientOptions clientOptions)
    {
        if (clientOptions.ChannelOptions is MqttClientTcpOptions tcpOptions1 &&
            _clientOptions.ChannelOptions is MqttClientTcpOptions tcpOptions2)
        {
            return tcpOptions1.Server.Equals(tcpOptions2.Server, StringComparison.OrdinalIgnoreCase) &&
                   tcpOptions1.Port == tcpOptions2.Port;
        }

        if (clientOptions.ChannelOptions is MqttClientWebSocketOptions webSocketOptions1 &&
            _clientOptions.ChannelOptions is MqttClientWebSocketOptions webSocketOptions2)
        {
            return webSocketOptions1.Uri.Equals(webSocketOptions2.Uri, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}
