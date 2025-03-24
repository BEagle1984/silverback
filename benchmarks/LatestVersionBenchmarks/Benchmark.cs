// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silverback.Configuration;
using Silverback.Messaging.Publishing;

namespace Silverback.Benchmarks.Latest;

public abstract class Benchmark
{
    private IPublisher? _publisher;

    private IHost? _host;

    protected IPublisher Publisher => _publisher ?? throw new InvalidOperationException("The publisher is not initialized yet.");

    protected IHost Host => _host ?? throw new InvalidOperationException("Not yet initialized.");

    [GlobalSetup]
    public virtual void Setup()
    {
        _host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices(
                services =>
                {
                    services.AddLogging(configure => configure.SetMinimumLevel(LogLevel.Error));
                    ConfigureServices(services);
                    ConfigureSilverback(services.AddSilverback());
                })
            .Build();

        Host.Start();

        _publisher = Host.Services.GetRequiredService<IPublisher>();
    }

    [GlobalCleanup]
    public virtual void Cleanup() => _host?.Dispose();

    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    protected virtual void ConfigureSilverback(SilverbackBuilder silverbackBuilder)
    {
    }
}
