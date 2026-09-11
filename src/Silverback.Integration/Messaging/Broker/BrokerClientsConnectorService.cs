// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Silverback.Messaging.Configuration;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

internal sealed class BrokerClientsConnectorService : IHostedLifecycleService
{
    private readonly BrokerClientConnectionOptions _clientConnectionOptions;

    private readonly IBrokerClientsConnector _connector;

    private readonly CancellationToken _applicationStoppingToken;

    private readonly Lazy<Task> _shutdownTask;

    private Task _stoppingTask = Task.CompletedTask;

    [SuppressMessage("Usage", "VSTHRD011:Use AsyncLazy<T>", Justification = "The host awaits shutdown asynchronously, no synchronous task waits are used")]
    public BrokerClientsConnectorService(
        BrokerClientConnectionOptions clientConnectionOptions,
        IHostApplicationLifetime applicationLifetime,
        IBrokerClientsConnector connector)
    {
        _connector = Check.NotNull(connector, nameof(connector));
        _clientConnectionOptions = Check.NotNull(clientConnectionOptions, nameof(clientConnectionOptions));
        _shutdownTask = new Lazy<Task>(ShutdownAsync);

        Check.NotNull(applicationLifetime, nameof(applicationLifetime));
        applicationLifetime.ApplicationStarted.Register(OnApplicationStarted);
        applicationLifetime.ApplicationStopping.Register(OnApplicationStopping);

        _applicationStoppingToken = applicationLifetime.ApplicationStopping;
    }

    public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _connector.InitializeAsync().ConfigureAwait(false);

        if (_clientConnectionOptions.Mode == BrokerClientConnectionMode.Startup)
            await _connector.ConnectAsync(_applicationStoppingToken).ConfigureAwait(false);
    }

    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => _shutdownTask.Value;

    private void OnApplicationStarted()
    {
        if (_clientConnectionOptions.Mode == BrokerClientConnectionMode.AfterStartup)
            _connector.ConnectAsync(_applicationStoppingToken).FireAndForget();
    }

    [SuppressMessage("ReSharper", "MethodSupportsCancellation", Justification = "Not needed")]
    private void OnApplicationStopping() => _stoppingTask = Task.Run(async () => await _connector.StopConsumersAsync().ConfigureAwait(false));

    private async Task ShutdownAsync()
    {
        await _stoppingTask.ConfigureAwait(false);
        await _connector.DisconnectAsync().ConfigureAwait(false);
    }
}
