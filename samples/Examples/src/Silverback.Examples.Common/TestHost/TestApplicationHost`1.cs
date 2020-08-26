// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Silverback.Examples.Common.TestHost
{
    public sealed class TestApplicationHost<TStartup> : ITestHost
        where TStartup : class
    {
        private readonly List<Action<IServiceCollection>>
            _configurationActions = new List<Action<IServiceCollection>>();

        private WebApplicationFactory<TStartup>? _applicationFactory;

        public IServiceProvider ServiceProvider =>
            _applicationFactory?.Services ?? throw new InvalidOperationException();

        public ITestHost ConfigureServices(Action<IServiceCollection> configurationAction)
        {
            _configurationActions.Add(configurationAction);

            return this;
        }

        public void Run()
        {
            if (_applicationFactory != null)
                throw new InvalidOperationException("This host is already running.");

            var appRoot = Path.Combine("src", GetType().Assembly.GetName().Name!);

            _applicationFactory = new TestApplicationFactory<TStartup>()
                .WithWebHostBuilder(
                    builder => builder
                        .ConfigureLogging(
                            loggingBuilder => loggingBuilder
                                .ClearProviders()
                                .SetMinimumLevel(LogLevel.Trace)
                                .AddSerilog())
                        .ConfigureServices(
                            services => _configurationActions.ForEach(configAction => configAction(services)))
                        .UseSolutionRelativeContentRoot(appRoot));

            _applicationFactory.CreateClient();

            _configurationActions.Clear();
        }

        public void Dispose()
        {
            _applicationFactory?.Server?.Host?.StopAsync();
            _applicationFactory?.Dispose();
            _applicationFactory = null;
        }
    }
}
