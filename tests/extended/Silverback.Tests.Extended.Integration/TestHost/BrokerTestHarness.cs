// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shouldly;
using Silverback.Configuration;
using Xunit.Abstractions;

namespace Silverback.Tests.Extended.Integration.TestHost;

// Builds the Silverback host of an in-process broker test, waits until the broker is ready for each step and tears everything down
// on dispose, writing the collected logs to the test output. The broker-specific harnesses add the readiness waits that a test would
// otherwise have to remember, so that the scenario only contains the steps it verifies.
public abstract class BrokerTestHarness : IAsyncDisposable
{
    private readonly List<Action<SilverbackBuilder>> _silverbackConfigurations = [];

    private readonly List<Action> _disposeActions = [];

    private readonly List<Func<Task>> _stoppedActions = [];

    private readonly List<Exception> _teardownExceptions = [];

    private IHost? _host;

    private Task? _hostStop;

    protected BrokerTestHarness(string name, ITestOutputHelper output)
    {
        Prefix = $"stress-{name}-{Guid.NewGuid():N}";
        Output = output;
    }

    // The unique prefix of the topics, client ids and groups of the test
    public string Prefix { get; }

    public TimeSpan Timeout { get; } = TimeSpan.FromSeconds(30);

    public ITestOutputHelper Output { get; }

    public CollectingLoggerProvider Logs { get; } = new();

    public IHost Host => _host ?? throw new InvalidOperationException("The host isn't built yet.");

    public IServiceProvider Services => Host.Services;

    protected bool IsHostBuilt => _host != null;

    protected bool IsHostStarted { get; private set; }

    // Adds the subscribers, callbacks and other services of the test
    public void ConfigureSilverback(Action<SilverbackBuilder> configure)
    {
        EnsureHostIsNotBuilt();
        _silverbackConfigurations.Add(configure);
    }

    // Runs the action first when disposing, for example to release a gate that blocks a subscriber and would block the shutdown
    public void OnDispose(Action action) => _disposeActions.Add(action);

    // Runs the action when disposing, once the Silverback clients are stopped, for example to stop and dispose the native clients of
    // the test, which would otherwise be disposed with the test method, before the harness. A failure fails the test as the
    // other teardown failures do.
    public void OnStopped(Func<Task> action) => _stoppedActions.Add(action);

    public virtual Task StartHostAsync()
    {
        IHost host = Host;
        IsHostStarted = true;

        return host.StartAsync().WaitAsync(Timeout);
    }

    // Stops the host only once and returns the same task when called again, without a timeout, so that the test can observe a
    // shutdown that is supposed to be blocked
    public Task StopHostAsync() => _hostStop ??= Host.StopAsync();

    public async Task WaitUntilAsync(Func<bool> condition, string description, TimeSpan? timeout = null)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        while (!condition())
        {
            stopwatch.Elapsed.ShouldBeLessThan(timeout ?? Timeout, $"Timed out waiting for {description}");
            await Task.Delay(100);
        }
    }

    // Runs every teardown step even if a previous one fails, writes the collected logs and the failures to the test output and then
    // rethrows the failures, so that a client that doesn't stop fails the test. Let xUnit dispose the harness (see
    // BrokerHarnessTests): it reports a teardown failure together with the failure of the test, while an exception thrown at the end
    // of an await using block would replace it.
    public async ValueTask DisposeAsync()
    {
        foreach (Action action in _disposeActions)
        {
            RunTeardownStep(action);
        }

        await RunTeardownStepAsync(StopClientsAsync);

        foreach (Func<Task> action in _stoppedActions)
        {
            await RunTeardownStepAsync(action);
        }

        await RunTeardownStepAsync(DisposeHostAsync);
        await RunTeardownStepAsync(DisposeResourcesAsync);

        foreach (LogEntry entry in Logs.Entries)
        {
            Output.WriteLine($"Log: {entry}");
        }

        foreach (Exception exception in _teardownExceptions)
        {
            Output.WriteLine($"Teardown failed: {exception}");
        }

        Logs.Dispose();
        GC.SuppressFinalize(this);

        if (_teardownExceptions.Count == 1)
            ExceptionDispatchInfo.Throw(_teardownExceptions[0]);

        if (_teardownExceptions.Count > 1)
            throw new AggregateException("The test harness teardown failed.", _teardownExceptions);
    }

    // Builds the host with the broker configuration and then the configuration of the test. The broker-specific services are
    // registered last, so that they replace the default ones.
    protected void BuildHost(Action<SilverbackBuilder> configureBroker, Action<IServiceCollection>? configureServices = null)
    {
        EnsureHostIsNotBuilt();

        HostApplicationBuilder builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders().AddProvider(Logs);

        SilverbackBuilder silverback = builder.Services.AddSilverback();
        configureBroker(silverback);

        foreach (Action<SilverbackBuilder> configure in _silverbackConfigurations)
        {
            configure(silverback);
        }

        configureServices?.Invoke(builder.Services);

        _host = builder.Build();
    }

    protected void EnsureHostIsNotBuilt()
    {
        if (_host != null)
            throw new InvalidOperationException("The host is already built, configure the harness before building it.");
    }

    // Stops the started host, before the host is disposed. Overrides also disconnect the clients connected without starting the
    // host.
    protected virtual async Task StopClientsAsync()
    {
        if (IsHostStarted)
            await StopHostAsync().WaitAsync(Timeout);
    }

    // Disposes the resources of the harness, after the host is disposed, since the host can still use them until then
    protected virtual Task DisposeResourcesAsync() => Task.CompletedTask;

    protected void RunTeardownStep(Action step)
    {
        try
        {
            step();
        }
        catch (Exception exception)
        {
            _teardownExceptions.Add(exception);
        }
    }

    protected async Task RunTeardownStepAsync(Func<Task> step)
    {
        try
        {
            await step();
        }
        catch (Exception exception)
        {
            _teardownExceptions.Add(exception);
        }
    }

    // Disposing the host disposes its clients, which can hang if they failed to stop, so the dispose is bounded by the timeout as
    // well, to let the teardown write the diagnostics anyway
    private Task DisposeHostAsync() =>
        _host switch
        {
            null => Task.CompletedTask,
            IAsyncDisposable asyncDisposable => asyncDisposable.DisposeAsync().AsTask().WaitAsync(Timeout),
            _ => Task.Run(_host.Dispose).WaitAsync(Timeout)
        };
}
