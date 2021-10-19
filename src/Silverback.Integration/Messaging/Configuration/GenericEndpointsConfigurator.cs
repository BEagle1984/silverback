// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Configuration;

internal sealed class GenericEndpointsConfigurator : IEndpointsConfigurator
{
    private readonly Action<EndpointsConfigurationBuilder> _configAction;

    public GenericEndpointsConfigurator(Action<EndpointsConfigurationBuilder> configAction)
    {
        _configAction = configAction;
    }

    public void Configure(EndpointsConfigurationBuilder builder) => _configAction(builder);
}
