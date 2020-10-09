// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private readonly IApplicationLifetime _applicationLifetime;

        private readonly IBrokerCollection _brokerCollection;

        private readonly BrokerConnectionOptions _connectionOptions;

        private readonly ISilverbackLogger<BrokerConnectorService> _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BrokerConnectorService" /> class.
        /// </summary>
        /// <param name="serviceScopeFactory">
        ///     The <see cref="IServiceScopeFactory" />.
        /// </param>
        /// <param name="applicationLifetime">
        ///     The <see cref="IApplicationLifetime" />.
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
            IServiceScopeFactory serviceScopeFactory,
            IApplicationLifetime applicationLifetime,
            IBrokerCollection brokersCollection,
            BrokerConnectionOptions connectionOptions,
            ISilverbackLogger<BrokerConnectorService> logger)
        {
            _serviceScopeFactory = Check.NotNull(serviceScopeFactory, nameof(serviceScopeFactory));
            _applicationLifetime = Check.NotNull(applicationLifetime, nameof(applicationLifetime));
            _brokerCollection = Check.NotNull(brokersCollection, nameof(brokersCollection));
            _connectionOptions = Check.NotNull(connectionOptions, nameof(connectionOptions));
            _logger = Check.NotNull(logger, nameof(logger));
        }

        /// <inheritdoc cref="BackgroundService.ExecuteAsync" />
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();

            _applicationLifetime.ApplicationStopping.Register(() => _brokerCollection.Disconnect());

            switch (_connectionOptions.Mode)
            {
                case BrokerConnectionMode.Startup:
                    return ConnectAsync(stoppingToken);
                case BrokerConnectionMode.AfterStartup:
                    _applicationLifetime.ApplicationStarted.Register(
                        async () => await ConnectAsync(stoppingToken).ConfigureAwait(false));
                    return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private async Task ConnectAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _brokerCollection.Connect();
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Log(
                        _connectionOptions.RetryOnFailure ? LogLevel.Error : LogLevel.Critical,
                        IntegrationEventIds.BrokerConnectionError,
                        ex,
                        "Error occurred connecting to the message broker(s).");

                    if (!_connectionOptions.RetryOnFailure)
                        break;

                    if (_connectionOptions.Mode == BrokerConnectionMode.Startup)
                        Thread.Sleep(_connectionOptions.RetryInterval);
                    else
                        await Task.Delay(_connectionOptions.RetryInterval, stoppingToken).ConfigureAwait(false);
                }
            }
        }
    }
}
