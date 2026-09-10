// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Configuration;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

internal sealed class BrokerClientsConnector : IBrokerClientsConnector, IDisposable
{
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Life cycle externally handled")]
    private readonly BrokerClientCollection _brokerClients;

    private readonly BrokerClientsBootstrapper _brokerClientsBootstrapper;

    private readonly BrokerClientConnectionOptions _clientConnectionOptions;

    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Life cycle externally handled")]
    private readonly ConsumerCollection _consumers;

    private readonly ISilverbackLogger<BrokerClientsConnectorService> _logger;

    private readonly SemaphoreSlim _connectSemaphore = new(1, 1);

    private bool _isInitialized;

    private bool _hasConnected;

    public BrokerClientsConnector(
        BrokerClientCollection brokerClients,
        BrokerClientsBootstrapper brokerClientsBootstrapper,
        BrokerClientConnectionOptions clientConnectionOptions,
        ConsumerCollection consumers,
        ISilverbackLogger<BrokerClientsConnectorService> logger)
    {
        _brokerClients = Check.NotNull(brokerClients, nameof(brokerClients));
        _brokerClientsBootstrapper = Check.NotNull(brokerClientsBootstrapper, nameof(brokerClientsBootstrapper));
        _clientConnectionOptions = Check.NotNull(clientConnectionOptions, nameof(clientConnectionOptions));
        _consumers = Check.NotNull(consumers, nameof(consumers));
        _logger = Check.NotNull(logger, nameof(logger));
    }

    /// <inheritdoc cref="IBrokerClientsConnector.InitializeAsync" />
    public async ValueTask InitializeAsync()
    {
        await _connectSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            await InitializeCoreAsync().ConfigureAwait(false);
        }
        finally
        {
            _connectSemaphore.Release();
        }
    }

    /// <inheritdoc cref="IBrokerClientsConnector.ConnectAsync" />
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    public async ValueTask ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _connectSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_hasConnected && _brokerClients.All(client => client.Status is ClientStatus.Initialized or ClientStatus.Initializing))
                return;

            await InitializeCoreAsync().ConfigureAwait(false);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await _brokerClients.ConnectAllAsync().ConfigureAwait(false);

                    await _brokerClientsBootstrapper.InvokeClientsConnectedCallbacksAsync().ConfigureAwait(false);

                    _hasConnected = true;
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogBrokerClientsInitializationError(ex);

                    if (!_clientConnectionOptions.RetryOnFailure)
                        break;
                }

                await DelayRetryAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _connectSemaphore.Release();
        }
    }

    /// <inheritdoc cref="IBrokerClientsConnector.StopConsumersAsync" />
    public async ValueTask StopConsumersAsync()
    {
        _logger.LogTrace("Stopping all consumers");
        await _consumers.StopAllAsync().ConfigureAwait(false);
        _logger.LogTrace("All consumers stopped");
    }

    /// <inheritdoc cref="IBrokerClientsConnector.DisconnectAsync" />
    public async ValueTask DisconnectAsync()
    {
        await _connectSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            _logger.LogTrace("Disconnecting all clients");
            await _brokerClients.DisconnectAllAsync().ConfigureAwait(false);
            _logger.LogTrace("All clients disconnected");

            _hasConnected = false;
        }
        finally
        {
            _connectSemaphore.Release();
        }
    }

    public void Dispose() => _connectSemaphore.Dispose();

    private async ValueTask InitializeCoreAsync()
    {
        if (_isInitialized)
            return;

        _isInitialized = true;

        await _brokerClientsBootstrapper.InitializeAllAsync().ConfigureAwait(false);
    }

    private async Task DelayRetryAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(_clientConnectionOptions.RetryInterval, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Ignore, the application is just shutting down
        }
    }
}
