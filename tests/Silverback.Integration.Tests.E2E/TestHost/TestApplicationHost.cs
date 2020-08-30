// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Tests.Integration.E2E.TestTypes.Database;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.TestHost
{
    public sealed class TestApplicationHost : IDisposable
    {
        private readonly List<Action<IServiceCollection>>
            _configurationActions = new List<Action<IServiceCollection>>();

        private bool _addDbContext;

        private SqliteConnection? _sqliteConnection;

        private WebApplicationFactory<BlankStartup>? _applicationFactory;

        private ITestOutputHelper? _testOutputHelper;

        private string? _testMethodName;

        public IServiceProvider ServiceProvider =>
            _applicationFactory?.Services ?? throw new InvalidOperationException();

        public TestApplicationHost ConfigureServices(Action<IServiceCollection> configurationAction)
        {
            _configurationActions.Add(configurationAction);

            return this;
        }

        public TestApplicationHost WithTestDbContext()
        {
            _addDbContext = true;

            _sqliteConnection = new SqliteConnection("DataSource=:memory:");
            _sqliteConnection.Open();

            return this;
        }

        public TestApplicationHost WithTestOutputHelper(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;

            return this;
        }

        public IServiceProvider Run([CallerMemberName] string? testMethodName = null)
        {
            if (_applicationFactory != null)
                throw new InvalidOperationException("Run can only be called once.");

            _testMethodName = testMethodName;

            var appRoot = Path.Combine("tests", GetType().Assembly.GetName().Name!);

            _applicationFactory = new TestApplicationFactory()
                .WithWebHostBuilder(
                    builder => builder
                        .ConfigureServices(
                            services =>
                            {
                                if (_testOutputHelper != null)
                                {
                                    services.AddLogging(
                                        configure =>
                                            configure
                                                .AddXUnit(_testOutputHelper)
                                                .SetMinimumLevel(LogLevel.Trace)
                                                .AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Information));
                                }

                                _configurationActions.ForEach(configAction => configAction(services));

                                if (_addDbContext)
                                {
                                    services.AddDbContext<TestDbContext>(
                                        options => options
                                            .UseSqlite(_sqliteConnection!));
                                }
                            })
                        .UseSolutionRelativeContentRoot(appRoot));

            _applicationFactory.CreateClient();

            if (_addDbContext)
                InitDatabase();

            _configurationActions.Clear();

            ServiceProvider.GetService<ILogger<TestApplicationHost>>()
                ?.LogInformation($"Starting end-to-end test {_testMethodName}.");

            return ServiceProvider.CreateScope().ServiceProvider;
        }

        public void PauseBackgroundServices() => ServiceProvider.PauseSilverbackBackgroundServices();

        public void ResumeBackgroundServices() => ServiceProvider.ResumeSilverbackBackgroundServices();

        public void Dispose()
        {
            ServiceProvider.GetService<ILogger<TestApplicationHost>>()
                ?.LogInformation($"Disposing test host ({_testMethodName}).");

            _sqliteConnection?.Dispose();
            _applicationFactory?.Dispose();
        }

        private void InitDatabase()
        {
            using (var scope = ServiceProvider.CreateScope())
            {
                scope.ServiceProvider.GetService<TestDbContext>()?.Database.EnsureCreated();
            }
        }
    }
}
