using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

            while (true)
            {
                Console.WriteLine(".");
                Thread.Sleep(1000);
            }
        }

        protected abstract void ConfigureServices(IServiceCollection services);

        protected abstract void Configure(IBrokerEndpointsConfigurationBuilder endpoints, IServiceProvider serviceProvider);
    }
}
