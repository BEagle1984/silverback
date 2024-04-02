// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Silverback.Messaging.Configuration;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

internal sealed class BrokerClientsConnectorService : IHostedService
{
    private readonly BrokerClientConnectionOptions _clientConnectionOptions;

    private readonly IBrokerClientsConnector _connector;

    private readonly CancellationToken _applicationStoppingToken;

    private readonly TaskCompletionSource<bool> _brokerDisconnectedTaskCompletionSource = new();

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
            await _connector.ConnectAllAsync(_applicationStoppingToken).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc cref="BackgroundService.ExecuteAsync" />
    private void OnApplicationStarted()
    {
        if (_clientConnectionOptions.Mode == BrokerClientConnectionMode.AfterStartup)
            _connector.ConnectAllAsync(_applicationStoppingToken).FireAndForget();
    }

    private void OnApplicationStopping() =>
        Task.Run(
                async () =>
                {
                    await _connector.DisconnectAllAsync().ConfigureAwait(false);
                    _brokerDisconnectedTaskCompletionSource.SetResult(true);
                })
            .FireAndForget();

    private void OnApplicationStopped() => AsyncHelper.RunSynchronously(() => _brokerDisconnectedTaskCompletionSource.Task);
}
