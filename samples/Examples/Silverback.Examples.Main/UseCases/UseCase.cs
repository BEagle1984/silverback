using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common;
using Silverback.Examples.Main.Menu;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;

namespace Silverback.Examples.Main.UseCases
{
    public abstract class UseCase : MenuItem
    {
        private const int ExecutionsCount = 3;

        protected UseCase(string name, int sortIndex = 100)
            : base(name, sortIndex)
        {
        }

        public void Execute()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Executing use case '{Name}'");
            Console.WriteLine();

            var services = DependencyInjectionHelper.GetServiceCollection();

            ConfigureServices(services);

            var serviceProvider = services.BuildServiceProvider();

            Configure(serviceProvider.GetService<IBrokerEndpointsConfigurationBuilder>());

            for (int i = 0; i < ExecutionsCount; i++)
            {
                CreateScopeAndExecute(serviceProvider);
            }
        }

        private void CreateScopeAndExecute(ServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                Execute(scope.ServiceProvider).Wait();
            }
        }

        protected abstract void ConfigureServices(IServiceCollection services);

        protected abstract void Configure(IBrokerEndpointsConfigurationBuilder endpoints);

        protected abstract Task Execute(IServiceProvider serviceProvider);
    }
}