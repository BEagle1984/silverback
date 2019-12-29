// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Configuration;

namespace Silverback.Examples.Common.Consumer
{
    public abstract class ConsumerApp
    {
        private IServiceProvider _serviceProvider;

        public void Init()
        {
            Console.WriteLine($"Initializing {GetType().Name}...");

            var services = DependencyInjectionHelper.GetServiceCollection();
            ConfigureServices(services);

            _serviceProvider = services.BuildServiceProvider();

            Configure(_serviceProvider.GetService<BusConfigurator>(), _serviceProvider);
        }

        protected abstract void ConfigureServices(IServiceCollection services);

        protected abstract void Configure(BusConfigurator configurator, IServiceProvider serviceProvider);
    }
}
