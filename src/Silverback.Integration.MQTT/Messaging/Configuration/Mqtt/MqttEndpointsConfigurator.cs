// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Configuration.Mqtt;

internal sealed class MqttEndpointsConfigurator : IEndpointsConfigurator
{
    private readonly Action<MqttEndpointsConfigurationBuilder> _configAction;

    public MqttEndpointsConfigurator(Action<MqttEndpointsConfigurationBuilder> configAction)
    {
        _configAction = configAction;
    }

    public void Configure(EndpointsConfigurationBuilder builder) => builder.AddMqttEndpoints(_configAction);
}
