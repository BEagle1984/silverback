// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Silverback.Tests
{
    public static class ServiceProviderHelper
    {
        public static IServiceProvider GetServiceProvider(
            Action<IServiceCollection> servicesConfigurationAction)
        {
            var services = new ServiceCollection();

            servicesConfigurationAction(services);

            var options = new ServiceProviderOptions { ValidateScopes = true };
            var serviceProvider = services.BuildServiceProvider(options);

            return serviceProvider.CreateScope().ServiceProvider;
        }
    }
}
