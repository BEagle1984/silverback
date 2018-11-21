using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Configuration;
using System;

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
