// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Silverback.Diagnostics;
using Silverback.Messaging.Configuration;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     Automatically connects the message brokers when the application starts and disconnects them when the
    ///     application is being stopped.
    /// </summary>
    public class BrokerConnectorService : BackgroundService
    {
        private readonly IBrokerCollection _brokerCollection;

        private readonly BrokerConnectionOptions _connectionOptions;

        private readonly ISilverbackLogger<BrokerConnectorService> _logger;

        private readonly CancellationToken _applicationStoppingToken;

        private readonly TaskCompletionSource<bool> _brokerDisconnectedTaskCompletionSource = new();

        /// <summary>
        ///     Initializes a new instance of the <see cref="BrokerConnectorService" /> class.
        /// </summary>
        /// <param name="applicationLifetime">
        ///     The <see cref="IHostApplicationLifetime" />.
        /// </param>
        /// <param name="brokersCollection">
        ///     The <see cref="IBrokerCollection" />.
        /// </param>
        /// <param name="connectionOptions">
        ///     The <see cref="BrokerConnectionOptions" />.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackLogger" />.
        /// </param>
        public BrokerConnectorService(
            IHostApplicationLifetime applicationLifetime,
            IBrokerCollection brokersCollection,
            BrokerConnectionOptions connectionOptions,
            ISilverbackLogger<BrokerConnectorService> logger)
        {
            _brokerCollection = Check.NotNull(brokersCollection, nameof(brokersCollection));
            _connectionOptions = Check.NotNull(connectionOptions, nameof(connectionOptions));
            _logger = Check.NotNull(logger, nameof(logger));

            Check.NotNull(applicationLifetime, nameof(applicationLifetime));
            applicationLifetime.ApplicationStarted.Register(OnApplicationStarted);
            applicationLifetime.ApplicationStopping.Register(OnApplicationStopping);
            applicationLifetime.ApplicationStopped.Register(OnApplicationStopped);

            _applicationStoppingToken = applicationLifetime.ApplicationStopping;
        }

        /// <inheritdoc cref="BackgroundService.ExecuteAsync" />
        protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
            _connectionOptions.Mode == BrokerConnectionMode.Startup
                ? ConnectAsync()
                : Task.CompletedTask;

        private void OnApplicationStarted()
        {
            if (_connectionOptions.Mode == BrokerConnectionMode.AfterStartup)
                ConnectAsync().FireAndForget();
        }

        private void OnApplicationStopping() =>
            Task.Run(
                    async () =>
                    {
                        await _brokerCollection.DisconnectAsync().ConfigureAwait(false);
                        _brokerDisconnectedTaskCompletionSource.SetResult(true);
                    })
                .FireAndForget();

        private void OnApplicationStopped() =>
            _brokerDisconnectedTaskCompletionSource.Task.Wait();

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private async Task ConnectAsync()
        {
            while (!_applicationStoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _brokerCollection.ConnectAsync().ConfigureAwait(false);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogBrokerConnectionError(ex);

                    if (!_connectionOptions.RetryOnFailure)
                        break;
                }

                await DelayRetryAsync().ConfigureAwait(false);
            }
        }

        private async Task DelayRetryAsync()
        {
            if (_connectionOptions.Mode == BrokerConnectionMode.Startup)
            {
                // We have to synchronously wait, to ensure the connection is awaited before starting
                Thread.Sleep(_connectionOptions.RetryInterval);
                return;
            }

            try
            {
                await Task.Delay(_connectionOptions.RetryInterval, _applicationStoppingToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Ignore, the application is just shutting down
            }
        }
    }
}
