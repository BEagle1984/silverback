// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Configuration;

namespace Silverback.Examples.Common.Consumer
{
    public abstract class ConsumerService
    {
        public void Init()
        {
            Console.WriteLine($"Initializing {GetType().Name}...");

            var services = DependencyInjectionHelper.GetServiceCollection();

            ConfigureServices(services);

            var serviceProvider = services.BuildServiceProvider();

            Configure(serviceProvider.GetService<IBrokerEndpointsConfigurationBuilder>(), serviceProvider);
        }

        protected abstract void ConfigureServices(IServiceCollection services);

        protected abstract void Configure(IBrokerEndpointsConfigurationBuilder endpoints, IServiceProvider serviceProvider);
    }
}
