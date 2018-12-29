// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Silverback.Examples.Common;
using Silverback.Examples.Common.Data;
using Silverback.Examples.Main.Menu;
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
            Console.ResetColor();

            var services = DependencyInjectionHelper.GetServiceCollection();

            ConfigureServices(services);

            using (var serviceProvider = services.BuildServiceProvider())
            {
                CreateScopeAndConfigure(serviceProvider);

                for (int i = 0; i < ExecutionsCount; i++)
                {
                    CreateScopeAndExecute(serviceProvider);
                }

                CreateScopeAndPostExecute(serviceProvider);
            }
        }

        private void CreateScopeAndConfigure(IServiceProvider serviceProvider)
        {
            ConfigureNLog(serviceProvider);

            serviceProvider.GetRequiredService<ExamplesDbContext>().Database.EnsureCreated();

            Configure(serviceProvider.GetService<IBrokerEndpointsConfigurationBuilder>(), serviceProvider);

            PreExecute(serviceProvider);
        }

        private static void ConfigureNLog(IServiceProvider serviceProvider)
        {
            serviceProvider.GetRequiredService<ILoggerFactory>()
                .AddNLog(new NLogProviderOptions { CaptureMessageTemplates = true, CaptureMessageProperties = true });
            NLog.LogManager.LoadConfiguration("nlog.config");
        }

        private void CreateScopeAndExecute(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                Execute(scope.ServiceProvider).Wait();
            }
        }

        private void CreateScopeAndPostExecute(IServiceProvider serviceProvider)
        {
            PostExecute(serviceProvider);
        }

        protected abstract void ConfigureServices(IServiceCollection services);

        protected abstract void Configure(IBrokerEndpointsConfigurationBuilder endpoints, IServiceProvider serviceProvider);

        protected abstract Task Execute(IServiceProvider serviceProvider);

        protected virtual void PreExecute(IServiceProvider serviceProvider) { }

        protected virtual void PostExecute(IServiceProvider serviceProvider) { }
    }
}