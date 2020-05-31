// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;

namespace Silverback.Examples.Consumer.HostedServices
{
    public sealed class ConsumersWatcher : IHostedService, IDisposable
    {
        private readonly IBrokerCollection _brokers;

        private readonly ILogger _logger;

        private Timer? _timer;

        public ConsumersWatcher(IBrokerCollection brokers, ILogger<ConsumersWatcher> logger)
        {
            _brokers = brokers;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(OnTimerTick, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Dispose();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private void OnTimerTick(object? state)
        {
            foreach (var broker in _brokers)
            {
                foreach (var consumer in broker.Consumers)
                {
                    if (!consumer.IsConnected)
                    {
                        _logger.LogInformation("Reconnecting consumer.");
                        consumer.Connect();
                    }
                }
            }
        }
    }
}
