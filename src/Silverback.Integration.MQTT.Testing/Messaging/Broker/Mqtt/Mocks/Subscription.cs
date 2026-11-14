// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using MQTTnet;
using MQTTnet.Protocol;
using Silverback.Messaging.Configuration.Mqtt;

namespace Silverback.Messaging.Broker.Mqtt.Mocks;

internal sealed class Subscription : ParsedTopic
{
    private readonly MqttClientOptions _clientOptions;

    public Subscription(MqttClientOptions clientOptions, string topic, MqttQualityOfServiceLevel qualityOfServiceLevel)
        : base(topic)
    {
        _clientOptions = clientOptions;
        Filter = topic;
        QualityOfServiceLevel = qualityOfServiceLevel;
    }

    // The subscribed topic filter, including the shared subscription prefix
    public string Filter { get; }

    // The maximum quality of service of the messages delivered through this subscription (always granted by the mocked broker)
    public MqttQualityOfServiceLevel QualityOfServiceLevel { get; }

    public bool IsMatch(MqttApplicationMessage message, MqttClientOptions clientOptions) =>
        IsSameBroker(clientOptions) && IsMatch(message.Topic);

    private bool IsSameBroker(MqttClientOptions clientOptions)
    {
        if (clientOptions.ChannelOptions is MqttClientTcpOptions tcpOptions1 &&
            _clientOptions.ChannelOptions is MqttClientTcpOptions tcpOptions2)
        {
            return tcpOptions1.RemoteEndpoint.Equals(tcpOptions2.RemoteEndpoint);
        }

        if (clientOptions.ChannelOptions is MqttClientWebSocketOptions webSocketOptions1 &&
            _clientOptions.ChannelOptions is MqttClientWebSocketOptions webSocketOptions2)
        {
            return webSocketOptions1.Uri.Equals(webSocketOptions2.Uri, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}
