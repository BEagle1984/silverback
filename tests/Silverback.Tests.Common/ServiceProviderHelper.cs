// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Silverback.Tests
{
    public static class ServiceProviderHelper
    {
        public static IServiceProvider GetServiceProvider(Action<IServiceCollection> servicesConfigurationAction)
        {
            var services = new ServiceCollection();

            services.AddNullLogger();

            servicesConfigurationAction(services);

            var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

            return serviceProvider.CreateScope().ServiceProvider;
        }
    }
}
