// Copyright (c) 2023 Sergio Aquilini
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

    private readonly BrokerClientsConfiguratorsInvoker _brokerClientsConfiguratorsInvoker;

    private readonly BrokerConnectionOptions _connectionOptions;

    private readonly ISilverbackLogger<BrokerClientsConnectorService> _logger;

    private bool _isInitialized;

    public BrokerClientsConnector(
        BrokerClientCollection brokerClients,
        BrokerClientsConfiguratorsInvoker brokerClientsConfiguratorsInvoker,
        BrokerConnectionOptions connectionOptions,
        ISilverbackLogger<BrokerClientsConnectorService> logger)
    {
        _brokerClients = Check.NotNull(brokerClients, nameof(brokerClients));
        _brokerClientsConfiguratorsInvoker = Check.NotNull(brokerClientsConfiguratorsInvoker, nameof(brokerClientsConfiguratorsInvoker));
        _connectionOptions = Check.NotNull(connectionOptions, nameof(connectionOptions));
        _logger = Check.NotNull(logger, nameof(logger));
    }

    /// <inheritdoc cref="IBrokerClientsConnector.InitializeAsync" />
    public async ValueTask InitializeAsync()
    {
        if (_isInitialized)
            return;

        _isInitialized = true;

        await _brokerClientsConfiguratorsInvoker.InvokeConfiguratorsAsync().ConfigureAwait(false);
    }

    /// <inheritdoc cref="IBrokerClientsConnector.ConnectAllAsync" />
    [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
    public async ValueTask ConnectAllAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync().ConfigureAwait(false);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _brokerClients.ConnectAllAsync().ConfigureAwait(false);

                break;
            }
            catch (Exception ex)
            {
                _logger.LogBrokerClientsInitializationError(ex);

                if (!_connectionOptions.RetryOnFailure)
                    break;
            }

            await DelayRetryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc cref="IBrokerClientsConnector.DisconnectAllAsync" />
    public ValueTask DisconnectAllAsync() => _brokerClients.DisconnectAllAsync();

    private async Task DelayRetryAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(_connectionOptions.RetryInterval, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Ignore, the application is just shutting down
        }
    }
}
