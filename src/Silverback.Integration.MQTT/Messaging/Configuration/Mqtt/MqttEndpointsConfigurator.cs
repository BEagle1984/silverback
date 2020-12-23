// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Configuration.Mqtt
{
    internal class MqttEndpointsConfigurator : IEndpointsConfigurator
    {
        private readonly Action<IMqttEndpointsConfigurationBuilder> _configAction;

        public MqttEndpointsConfigurator(Action<IMqttEndpointsConfigurationBuilder> configAction)
        {
            _configAction = configAction;
        }

        public void Configure(IEndpointsConfigurationBuilder builder) => builder.AddMqttEndpoints(_configAction);
    }
}
