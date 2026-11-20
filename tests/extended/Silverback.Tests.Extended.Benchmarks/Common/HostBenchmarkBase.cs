// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silverback.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Tests.Extended.Benchmarks.Common;

/// <summary>
///     The base class of the benchmarks running against a host with mocked brokers. A new host is started before every iteration and
///     disposed after it, so that the state accumulated by the mocks (topics, outbox) never grows across iterations.
/// </summary>
/// <remarks>
///     The derived classes are expected to use <c>RunStrategy.Monitoring</c> (see <see cref="HostBenchmarkConfig" />) and to perform a
///     bounded number of operations per invocation.
/// </remarks>
public abstract class HostBenchmarkBase
{
    private IHost? _host;

    private IServiceScope? _scope;

    protected IServiceProvider ScopedServices => _scope?.ServiceProvider ?? throw new InvalidOperationException("The host is not started.");

    protected IPublisher Publisher
    {
        get => field ?? throw new InvalidOperationException("The host is not started.");
        private set;
    }

    [IterationSetup]
    public void StartHost()
    {
        StopHost();

        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings());
        builder.Services.AddLogging();
        ConfigureSilverback(builder.Services.AddSilverback());

        _host = builder.Build();
        _host.StartAsync().SafeWait();

        _scope = _host.Services.CreateScope();
        Publisher = _scope.ServiceProvider.GetRequiredService<IPublisher>();

        OnHostStarted();
    }

    [IterationCleanup]
    [GlobalCleanup]
    public void StopHost()
    {
        Publisher = null!;
        _scope?.Dispose();
        _scope = null;

        if (_host == null)
            return;

        _host.StopAsync().SafeWait();
        _host.Dispose();
        _host = null;
    }

    protected abstract void ConfigureSilverback(SilverbackBuilder silverbackBuilder);

    protected virtual void OnHostStarted()
    {
    }
}
