// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Configuration.Mqtt;

internal sealed class MqttBrokerClientsConfigurator : IBrokerClientsConfigurator
{
    private readonly Action<MqttClientsConfigurationBuilder> _configAction;

    public MqttBrokerClientsConfigurator(Action<MqttClientsConfigurationBuilder> configAction)
    {
        _configAction = configAction;
    }

    public void Configure(BrokerClientsConfigurationBuilder builder) => builder.AddMqttClients(_configAction);
}
