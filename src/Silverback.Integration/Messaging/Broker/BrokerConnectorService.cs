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

        private readonly IBrokerCollection _brokers;

        private readonly ISilverbackLogger<BrokerConnectorService> _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BrokerConnectorService" /> class.
        /// </summary>
        /// <param name="serviceScopeFactory">The <see cref="IServiceScopeFactory" />.</param>
        /// <param name="applicationLifetime">The <see cref="IApplicationLifetime" />.</param>
        /// <param name="brokers">The <see cref="IBrokerCollection" />.</param>
        /// <param name="logger">The <see cref="ISilverbackLogger" />.</param>
        public BrokerConnectorService(
            IServiceScopeFactory serviceScopeFactory,
            IApplicationLifetime applicationLifetime,
            IBrokerCollection brokers,
            ISilverbackLogger<BrokerConnectorService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _applicationLifetime = applicationLifetime;
            _brokers = brokers;
            _logger = logger;
        }

        /// <inheritdoc cref="BackgroundService.ExecuteAsync" />
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();

            var options = scope.ServiceProvider.GetRequiredService<BrokerConnectionOptions>();

            _applicationLifetime.ApplicationStopping.Register(() => _brokers.Disconnect());

            switch (options.Mode)
            {
                case BrokerConnectionMode.Startup:
                    return Connect(options, stoppingToken);
                case BrokerConnectionMode.AfterStartup:
#pragma warning disable 4014
                    _applicationLifetime.ApplicationStarted.Register(() => Connect(options, stoppingToken));
#pragma warning restore 4014
                    return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private async Task Connect(BrokerConnectionOptions options, CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _brokers.Connect();
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Log(
                        options.RetryOnFailure ? LogLevel.Error : LogLevel.Critical,
                        IntegrationEventIds.BrokerConnectionError,
                        ex,
                        "Error occurred connecting to the message broker(s).");

                    if (!options.RetryOnFailure)
                        break;

                    if (options.Mode == BrokerConnectionMode.Startup)
                        Thread.Sleep(options.RetryInterval);
                    else
                        await Task.Delay(options.RetryInterval, stoppingToken).ConfigureAwait(false);
                }
            }
        }
    }
}
