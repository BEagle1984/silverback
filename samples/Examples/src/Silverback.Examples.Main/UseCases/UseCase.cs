// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Examples.Common.TestHost;
using Silverback.Examples.Main.Menu;

namespace Silverback.Examples.Main.UseCases
{
    public abstract class UseCase : IUseCase
    {
        /// <inheritdoc cref="IMenuItemInfo" />
        public string? Title { get; protected set; }

        /// <inheritdoc cref="IMenuItemInfo" />
        public string? Description { get; protected set; }

        /// <summary>
        ///     Gets or sets the number of times the runnable use case has to be called.
        /// </summary>
        protected int ExecutionsCount { get; set; } = 3;

        /// <summary>
        ///     Runs the use case.
        /// </summary>
        public void Run()
        {
            var startupTypeName = GetTypeName("Startup");
            var startupType = Type.GetType(startupTypeName) ??
                              throw new InvalidOperationException($"Type {startupTypeName} not found.");

            var runTypeName = GetTypeName("Run");
            var runType = Type.GetType(runTypeName) ??
                          throw new InvalidOperationException($"Type {runTypeName} not found.");

            if (!typeof(IAsyncRunnable).IsAssignableFrom(runType))
                throw new InvalidOperationException($"The type '{runTypeName}' doesn't implement IAsyncRunnable.");

            var host = (ITestHost?)Activator.CreateInstance(typeof(TestApplicationHost<>).MakeGenericType(startupType))
                       ?? throw new InvalidOperationException("Couldn't activate host.");

            try
            {
                host.ConfigureServices(services => services.AddScoped(runType));
                host.Run();

                for (int i = 0; i < ExecutionsCount; i++)
                {
                    using var scope = host.ServiceProvider.CreateScope();
                    var runnable = (IAsyncRunnable)scope.ServiceProvider.GetRequiredService(runType);
                    runnable.Run().Wait();
                }
            }
            finally
            {
                host.Dispose();
            }
        }

        private string GetTypeName(string suffix) => GetType().FullName! + suffix;
    }
}
