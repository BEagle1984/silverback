// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Configuration;

internal sealed class GenericBrokerClientsConfigurator : IBrokerClientsConfigurator
{
    private readonly Action<BrokerClientsConfigurationBuilder> _configAction;

    public GenericBrokerClientsConfigurator(Action<BrokerClientsConfigurationBuilder> configAction)
    {
        _configAction = configAction;
    }

    public void Configure(BrokerClientsConfigurationBuilder builder) => _configAction(builder);
}
