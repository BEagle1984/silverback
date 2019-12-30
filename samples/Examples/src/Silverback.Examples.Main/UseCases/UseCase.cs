// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common;
using Silverback.Examples.Common.Data;
using Silverback.Examples.Common.Logging;
using Silverback.Examples.Main.Menu;
using Silverback.Messaging.Configuration;

namespace Silverback.Examples.Main.UseCases
{
    public abstract class UseCase : IUseCase
    {
        /// <inheritdoc cref="IMenuItemInfo"/>
        public string Title { get; protected set; }

        /// <inheritdoc cref="IMenuItemInfo"/>
        public string Description { get; protected set; }

        /// <summary>
        /// Gets or sets the number of times the <see cref="Execute"/> method has to be called in a loop.
        /// </summary>
        protected int ExecutionsCount { get; set; } = 3;

        /// <summary>
        /// Runs the use case.
        /// </summary>
        public void Run()
        {
            var services = DependencyInjectionHelper.GetServiceCollection();

            ConfigureServices(services);

            var serviceProvider = BuildServiceProvider(services);

            try
            {
                CreateScopeAndConfigure(serviceProvider);

                for (int i = 0; i < ExecutionsCount; i++)
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

        private IServiceProvider BuildServiceProvider(IServiceCollection services) =>
            services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateScopes = true
            });

        private void CreateScopeAndConfigure(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            LoggingConfiguration.Setup();

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

        /// <summary>
        /// <param>Registers the required services for DI.</param>
        /// <param>(Same to what is normally done in the Startup.cs.)</param>
        /// </summary>
        protected abstract void ConfigureServices(IServiceCollection services);

        /// <summary>
        /// <param>Configures the Bus.</param>
        /// <param>(Same to what is normally done in the Startup.cs.)</param>
        /// </summary>
        protected abstract void Configure(BusConfigurator configurator, IServiceProvider serviceProvider);

        /// <summary>
        /// Executes the necessary logic to demonstrate the use case.
        /// </summary>
        protected abstract Task Execute(IServiceProvider serviceProvider);

        /// <summary>
        /// Invoked after the configuration and just before <see cref="Execute"/>.
        /// Can be used for example to setup the use case data.
        /// </summary>
        protected virtual void PreExecute(IServiceProvider serviceProvider) { }

        /// <summary>
        /// Invoked after all runs of the <see cref="Execute"/> method.
        /// Can be used to execute some cleanup work, if needed.
        /// </summary>
        protected virtual void PostExecute(IServiceProvider serviceProvider) { }
    }
}