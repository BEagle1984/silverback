// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace Silverback.Tests
{
    public static class ServiceProviderHelper
    {
        public static IServiceProvider GetServiceProvider(
            Action<IServiceCollection> servicesConfigurationAction,
            IHostApplicationLifetime? hostApplicationLifetime = null)
        {
            var services = new ServiceCollection();
            services.AddSingleton(hostApplicationLifetime ?? Substitute.For<IHostApplicationLifetime>());

            servicesConfigurationAction(services);

            var options = new ServiceProviderOptions { ValidateScopes = true };
            var serviceProvider = services.BuildServiceProvider(options);

            return serviceProvider.CreateScope().ServiceProvider;
        }
    }
}
