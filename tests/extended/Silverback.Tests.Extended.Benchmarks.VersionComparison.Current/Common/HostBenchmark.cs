// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silverback.Configuration;
using Silverback.Messaging.Publishing;

namespace Silverback.Tests.Extended.Benchmarks.VersionComparison.Current.Common;

/// <summary>
///     The base class of the benchmarks running against a host with mocked brokers. A new host is started before every iteration and
///     disposed after it, so that the messages accumulated by the mocked topics never grow across iterations.
/// </summary>
/// <remarks>
///     The derived classes are expected to use <see cref="HostBenchmarkConfig" /> and to perform a bounded number of operations per
///     invocation.
/// </remarks>
public abstract class HostBenchmark
{
    private IHost? _host;

    private IServiceScope? _scope;

    protected IServiceProvider Services => _scope?.ServiceProvider ?? throw new InvalidOperationException("The host is not started.");

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
        _host.StartAsync().GetAwaiter().GetResult();

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

        _host.StopAsync().GetAwaiter().GetResult();
        _host.Dispose();
        _host = null;
    }

    protected abstract void ConfigureSilverback(SilverbackBuilder silverbackBuilder);

    protected virtual void OnHostStarted()
    {
    }
}
