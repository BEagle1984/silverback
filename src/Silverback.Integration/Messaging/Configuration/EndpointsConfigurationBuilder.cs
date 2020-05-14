// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Configuration
{
    internal class EndpointsConfigurationBuilder : IEndpointsConfigurationBuilder
    {
        public EndpointsConfigurationBuilder(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public IServiceProvider ServiceProvider { get; }
    }
}
