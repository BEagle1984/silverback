// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Configuration;
using Silverback.Testing;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.TestHost;

public sealed class TestApplicationHost : IDisposable
{
    private readonly List<Action<IServiceCollection>> _configurationActions = [];

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
                                            .AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Information));
                            }

                            _configurationActions.ForEach(configAction => configAction(services));
                        })
                    .UseSolutionRelativeContentRoot(appRoot));

        _httpClient = _applicationFactory.CreateClient();

        _configurationActions.Clear();

        ILogger<TestApplicationHost>? logger = ScopedServiceProvider.GetService<ILogger<TestApplicationHost>>();

        if (waitUntilBrokerClientsConnected)
        {
            logger?.LogInformation("Waiting for broker clients to connect...");
            await WaitUntilBrokerClientsAreConnectedAsync();
        }

        logger?.LogInformation("Starting end-to-end test {TestMethod}", _testMethodName);
    }

    public void Dispose()
    {
        ILogger<TestApplicationHost>? logger = _scopedServiceProvider?.GetService<ILogger<TestApplicationHost>>();
        logger?.LogInformation("Disposing test host ({TestMethod})", _testMethodName);

        _httpClient?.Dispose();
        _applicationFactory?.Dispose();

        logger?.LogInformation("Test host disposed ({TestMethod})", _testMethodName);
    }

    private async ValueTask WaitUntilBrokerClientsAreConnectedAsync()
    {
        BrokerClientConnectionOptions clientConnectionOptions = ServiceProvider.GetRequiredService<BrokerClientConnectionOptions>();

        if (clientConnectionOptions.Mode == BrokerClientConnectionMode.Manual)
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
}
