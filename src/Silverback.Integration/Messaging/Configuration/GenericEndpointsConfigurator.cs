// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Configuration
{
    internal sealed class GenericEndpointsConfigurator : IEndpointsConfigurator
    {
        private readonly Action<IEndpointsConfigurationBuilder> _configAction;

        public GenericEndpointsConfigurator(Action<IEndpointsConfigurationBuilder> configAction)
        {
            _configAction = configAction;
        }

        public void Configure(IEndpointsConfigurationBuilder builder) => _configAction(builder);
    }
}
