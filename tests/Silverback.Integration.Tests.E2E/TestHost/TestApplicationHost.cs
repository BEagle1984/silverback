// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Configuration;
using Silverback.Testing;
using Silverback.Tests.Integration.E2E.TestTypes.Database;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.TestHost;

public sealed class TestApplicationHost : IDisposable
{
    private readonly List<Action<IServiceCollection>> _configurationActions = new();

    private readonly string _sqliteConnectionString = $"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared";

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

    public string SqliteConnectionString
    {
        get
        {
            if (_sqliteConnection == null)
            {
                _sqliteConnection = new SqliteConnection(_sqliteConnectionString);
                _sqliteConnection.Open();
            }

            return _sqliteConnectionString;
        }
    }

    public ValueTask ConfigureServicesAndRunAsync(
        Action<IServiceCollection> configurationAction,
        [CallerMemberName] string? testMethodName = null) =>
        ConfigureServices(configurationAction).RunAsync(testMethodName);

    public TestApplicationHost ConfigureServices(Action<IServiceCollection> configurationAction)
    {
        _configurationActions.Add(configurationAction);

        return this;
    }

    public TestApplicationHost WithTestOutputHelper(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;

        return this;
    }

    public async ValueTask RunAsync([CallerMemberName] string? testMethodName = null, bool waitUntilBrokerClientsConnected = true)
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
                        })
                    .UseSolutionRelativeContentRoot(appRoot));

        _httpClient = _applicationFactory.CreateClient();

        if (_sqliteConnection != null)
            InitDatabase();

        _configurationActions.Clear();

        ILogger<TestApplicationHost>? logger = ScopedServiceProvider.GetService<ILogger<TestApplicationHost>>();

        if (waitUntilBrokerClientsConnected)
        {
            logger?.LogInformation("Waiting for broker clients to connect...");
            await WaitUntilBrokerClientsAreConnectedAsync();
        }

        logger?.LogInformation("Starting end-to-end test {testMethod}", _testMethodName);
    }

    public void Dispose()
    {
        ILogger<TestApplicationHost>? logger = _scopedServiceProvider?.GetService<ILogger<TestApplicationHost>>();
        logger?.LogInformation("Disposing test host ({testMethod})", _testMethodName);

        _sqliteConnection?.Dispose();
        _httpClient?.Dispose();
        _applicationFactory?.Dispose();

        logger?.LogInformation("Test host disposed ({testMethod})", _testMethodName);
    }

    private async ValueTask WaitUntilBrokerClientsAreConnectedAsync()
    {
        BrokerConnectionOptions connectionOptions = ServiceProvider.GetRequiredService<BrokerConnectionOptions>();

        if (connectionOptions.Mode == BrokerConnectionMode.Manual)
            return;

        await WaitUntilBrokerClientsAreConnectedAsync<IKafkaTestingHelper>();
        await WaitUntilBrokerClientsAreConnectedAsync<IMqttTestingHelper>();
    }

    private ValueTask WaitUntilBrokerClientsAreConnectedAsync<THelper>()
        where THelper : ITestingHelper
    {
        THelper? helper = ServiceProvider.GetService<THelper>();
        return helper != null ? helper.WaitUntilConnectedAsync() : default;
    }

    private void InitDatabase()
    {
        using IServiceScope scope = ServiceProvider.CreateScope();
        scope.ServiceProvider.GetService<TestDbContext>()?.Database.EnsureCreated();
    }
}
