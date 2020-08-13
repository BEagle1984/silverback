// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Silverback.Tests.Integration.E2E.TestHost
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Testing framework")]
    public sealed class TestApplicationHost : IDisposable
    {
        private readonly List<Action<IServiceCollection>>
            _configurationActions = new List<Action<IServiceCollection>>();

        private WebApplicationFactory<BlankStartup>? _applicationFactory;

        public IServiceProvider ServiceProvider => _applicationFactory?.Services ?? throw new InvalidOperationException();

        public TestApplicationHost ConfigureServices(Action<IServiceCollection> configurationAction)
        {
            _configurationActions.Add(configurationAction);

            return this;
        }

        public IServiceProvider Run()
        {
            var appRoot = Path.Combine("tests", GetType().Assembly.GetName().Name!);

            _applicationFactory = new TestApplicationFactory()
                .WithWebHostBuilder(
                    builder => builder
                        .ConfigureServices(
                            services => _configurationActions.ForEach(configAction => configAction(services)))
                        .UseSolutionRelativeContentRoot(appRoot));

            _applicationFactory.CreateClient();

            _configurationActions.Clear();

            return ServiceProvider.CreateScope().ServiceProvider;
        }

        public void PauseBackgroundServices() => ServiceProvider.PauseSilverbackBackgroundServices();

        public void ResumeBackgroundServices() => ServiceProvider.ResumeSilverbackBackgroundServices();

        public void Dispose()
        {
            _applicationFactory?.Dispose();
        }
    }
}
