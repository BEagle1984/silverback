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

internal sealed class BrokerClientsConnectorService : IHostedService, IDisposable
{
    private readonly BrokerClientConnectionOptions _clientConnectionOptions;

    private readonly IBrokerClientsConnector _connector;

    private readonly CancellationToken _applicationStoppingToken;

    private readonly SemaphoreSlim _consumersStoppedSemaphore = new(0);

    private readonly SemaphoreSlim _clientsDisconnectedSemaphore = new(0);

    public BrokerClientsConnectorService(
        BrokerClientConnectionOptions clientConnectionOptions,
        IHostApplicationLifetime applicationLifetime,
        IBrokerClientsConnector connector)
    {
        _connector = Check.NotNull(connector, nameof(connector));
        _clientConnectionOptions = Check.NotNull(clientConnectionOptions, nameof(clientConnectionOptions));

        Check.NotNull(applicationLifetime, nameof(applicationLifetime));
        applicationLifetime.ApplicationStarted.Register(OnApplicationStarted);
        applicationLifetime.ApplicationStopping.Register(OnApplicationStopping);
        applicationLifetime.ApplicationStopped.Register(OnApplicationStopped);

        _applicationStoppingToken = applicationLifetime.ApplicationStopping;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _connector.InitializeAsync().ConfigureAwait(false);

        if (_clientConnectionOptions.Mode == BrokerClientConnectionMode.Startup)
            await _connector.ConnectAsync(_applicationStoppingToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void Dispose()
    {
        _consumersStoppedSemaphore.Dispose();
        _clientsDisconnectedSemaphore.Dispose();
    }

    private void OnApplicationStarted()
    {
        if (_clientConnectionOptions.Mode == BrokerClientConnectionMode.AfterStartup)
            _connector.ConnectAsync(_applicationStoppingToken).FireAndForget();
    }

    [SuppressMessage("ReSharper", "MethodSupportsCancellation", Justification = "Not needed")]
    private void OnApplicationStopping() =>
        Task.Run(async () =>
            {
                try
                {
                    await _connector.StopConsumersAsync().ConfigureAwait(false);
                }
                finally
                {
                    _consumersStoppedSemaphore.Release();
                }
            })
            .FireAndForget();

    [SuppressMessage("ReSharper", "MethodSupportsCancellation", Justification = "Not needed")]
    private void OnApplicationStopped()
    {
        Task.Run(async () =>
            {
                try
                {
                    await _consumersStoppedSemaphore.WaitAsync().ConfigureAwait(false);
                    await _connector.DisconnectAsync().ConfigureAwait(false);
                }
                finally
                {
                    _clientsDisconnectedSemaphore.Release();
                }
            })
            .FireAndForget();

        _clientsDisconnectedSemaphore.Wait();
    }
}
