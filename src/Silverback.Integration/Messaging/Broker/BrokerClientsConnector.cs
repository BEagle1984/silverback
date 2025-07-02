// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Configuration;
using Silverback.Util;

namespace Silverback.Messaging.Broker;

internal class BrokerClientsConnector : IBrokerClientsConnector
{
    private readonly BrokerClientCollection _brokerClients;

    private readonly BrokerClientsBootstrapper _brokerClientsBootstrapper;

    private readonly BrokerClientConnectionOptions _clientConnectionOptions;

    private readonly ConsumerCollection _consumers;

    private readonly ISilverbackLogger<BrokerClientsConnectorService> _logger;

    private bool _isInitialized;

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
        if (_isInitialized)
            return;

        _isInitialized = true;

        await _brokerClientsBootstrapper.InitializeAllAsync().ConfigureAwait(false);
    }

    /// <inheritdoc cref="IBrokerClientsConnector.ConnectAllAsync" />
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
    public async ValueTask ConnectAllAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync().ConfigureAwait(false);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _brokerClients.ConnectAllAsync().ConfigureAwait(false);

                await _brokerClientsBootstrapper.InvokeClientsConnectedCallbacksAsync().ConfigureAwait(false);

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

    /// <inheritdoc cref="IBrokerClientsConnector.DisconnectAllAsync" />
    public async ValueTask DisconnectAllAsync()
    {
        await _consumers.StopAllAsync().ConfigureAwait(false);
        _logger.LogTrace("All consumers stopped.");
        await _brokerClients.DisconnectAllAsync().ConfigureAwait(false);
        _logger.LogTrace("All clients disconnected.");
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
