// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Examples.Common;
using Silverback.Examples.Common.Data;
using Silverback.Examples.Main.Menu;
using Silverback.Messaging.Configuration;

namespace Silverback.Examples.Main.UseCases
{
    public abstract class UseCase : MenuItem
    {
        private const bool UseAutofac = false;

        private readonly int _executionsCount;

        protected UseCase(string name, int sortIndex = 100, int executionCount = 3)
            : base(name, sortIndex)
        {
            _executionsCount = executionCount;
        }

        public void Execute()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Executing use case '{Name}'");
            Console.WriteLine();
            Console.ResetColor();

            var services = DependencyInjectionHelper.GetServiceCollection();

            ConfigureServices(services);

            var serviceProvider = BuildServiceProvider(services);

            try
            {
                CreateScopeAndConfigure(serviceProvider);

                for (int i = 0; i < _executionsCount; i++)
                {
                    CreateScopeAndExecute(serviceProvider);
                }

                CreateScopeAndPostExecute(serviceProvider);
            }
            finally
            {
                ((IDisposable) serviceProvider)?.Dispose();
            }
        }

        private IServiceProvider BuildServiceProvider(IServiceCollection services)
        {
            if (UseAutofac)
            {
                return ConfigureAutofac(services);
            }
            else
            {
                return services.BuildServiceProvider(new ServiceProviderOptions
                {
                    ValidateScopes = true
                });
            }
        }

        private IServiceProvider ConfigureAutofac(IServiceCollection services)
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterAssemblyModules(Assembly.GetExecutingAssembly());
            containerBuilder.Populate(services);
            return new AutofacServiceProvider(containerBuilder.Build());
        }

        private void CreateScopeAndConfigure(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            Configuration.SetupSerilog();

            scope.ServiceProvider.GetRequiredService<ExamplesDbContext>().Database.EnsureCreated();

            Configure(scope.ServiceProvider.GetService<BusConfigurator>(), scope.ServiceProvider);

            PreExecute(scope.ServiceProvider);
        }

        private void CreateScopeAndExecute(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            Execute(scope.ServiceProvider).Wait();
        }

        private void CreateScopeAndPostExecute(IServiceProvider serviceProvider)
        {
            PostExecute(serviceProvider);
        }

        protected abstract void ConfigureServices(IServiceCollection services);

        protected abstract void Configure(BusConfigurator configurator, IServiceProvider serviceProvider);

        protected abstract Task Execute(IServiceProvider serviceProvider);

        protected virtual void PreExecute(IServiceProvider serviceProvider) { }

        protected virtual void PostExecute(IServiceProvider serviceProvider) { }
    }
}