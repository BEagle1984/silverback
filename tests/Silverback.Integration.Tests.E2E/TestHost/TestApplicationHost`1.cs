// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Testing;
using Silverback.Tests.Integration.E2E.TestTypes.Database;
using Silverback.Util;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.TestHost;

public sealed class TestApplicationHost<THelper> : IDisposable
    where THelper : ITestingHelper<IBroker>
{
    private readonly List<Action<IServiceCollection>> _configurationActions = new();

    private SqliteConnection? _sqliteConnection;

    private WebApplicationFactory<Startup>? _applicationFactory;

    private ITestOutputHelper? _testOutputHelper;

    private string? _testMethodName;

    private IServiceProvider? _scopedServiceProvider;

    private HttpClient? _httpClient;

    public IServiceProvider ServiceProvider =>
        _applicationFactory?.Services ?? throw new InvalidOperationException();

    public IServiceProvider ScopedServiceProvider =>
        _scopedServiceProvider ??= ServiceProvider.CreateScope().ServiceProvider;

    public HttpClient HttpClient => _httpClient ?? throw new InvalidOperationException();

    public TestApplicationHost<THelper> ConfigureServices(Action<IServiceCollection> configurationAction)
    {
        _configurationActions.Add(configurationAction);

        return this;
    }

    public TestApplicationHost<THelper> WithTestDbContext()
    {
        _sqliteConnection = new SqliteConnection($"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        _sqliteConnection.Open();

        return this;
    }

    public TestApplicationHost<THelper> WithTestOutputHelper(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;

        return this;
    }

    public void Run([CallerMemberName] string? testMethodName = null, bool waitUntilBrokerConnected = true)
    {
        if (_applicationFactory != null)
            throw new InvalidOperationException("Run can only be called once.");

        _testMethodName = testMethodName;

        string appRoot = Path.Combine("tests", GetType().Assembly.GetName().Name!);

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
                                            .AddFilter(
                                                "Microsoft.EntityFrameworkCore",
                                                LogLevel.Information));
                            }

                            _configurationActions.ForEach(configAction => configAction(services));

                            if (_sqliteConnection != null)
                            {
                                services.AddDbContext<TestDbContext>(
                                    options => options
                                        .UseSqlite(_sqliteConnection.ConnectionString));
                            }
                        })
                    .UseSolutionRelativeContentRoot(appRoot));

        _httpClient = _applicationFactory.CreateClient();

        if (_sqliteConnection != null)
            InitDatabase();

        _configurationActions.Clear();

        if (waitUntilBrokerConnected)
            WaitUntilBrokerIsConnected();

        ScopedServiceProvider.GetService<ILogger<TestApplicationHost<THelper>>>()
            ?.LogInformation($"Starting end-to-end test {_testMethodName}.");
    }

    public void PauseBackgroundServices() => ServiceProvider.PauseSilverbackBackgroundServices();

    public void ResumeBackgroundServices() => ServiceProvider.ResumeSilverbackBackgroundServices();

    public void Dispose()
    {
        ILogger<TestApplicationHost<THelper>>? logger = _scopedServiceProvider?.GetService<ILogger<TestApplicationHost<THelper>>>();
        logger?.LogInformation($"Disposing test host ({_testMethodName}).");

        _sqliteConnection?.SafeClose();
        _sqliteConnection?.Dispose();
        _httpClient?.Dispose();
        _applicationFactory?.Dispose();

        logger?.LogInformation($"Test host disposed ({_testMethodName}).");
    }

    private void WaitUntilBrokerIsConnected()
    {
        BrokerConnectionOptions connectionOptions = ServiceProvider.GetRequiredService<BrokerConnectionOptions>();

        if (connectionOptions.Mode == BrokerConnectionMode.Manual)
            return;

        AsyncHelper.RunSynchronously(() => ServiceProvider.GetRequiredService<THelper>().WaitUntilConnectedAsync());
    }

    private void InitDatabase()
    {
        using IServiceScope scope = ServiceProvider.CreateScope();
        scope.ServiceProvider.GetService<TestDbContext>()?.Database.EnsureCreated();
    }
}
