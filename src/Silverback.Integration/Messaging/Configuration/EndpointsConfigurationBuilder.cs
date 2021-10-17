// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    internal sealed class EndpointsConfigurationBuilder : IEndpointsConfigurationBuilder
    {
        public EndpointsConfigurationBuilder(IServiceProvider serviceProvider)
        {
            ServiceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
        }

        public IServiceProvider ServiceProvider { get; }
    }
}
