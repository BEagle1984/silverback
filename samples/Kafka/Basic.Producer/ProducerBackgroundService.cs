using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Publishing;
using Silverback.Samples.Kafka.Basic.Common;

namespace Silverback.Samples.Kafka.Basic.Producer
{
    public class ProducerBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private readonly ILogger<ProducerBackgroundService> _logger;

        public ProducerBackgroundService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<ProducerBackgroundService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Create a service scope and resolve the IPublisher
            // (the IPublisher cannot be resolved from the root scope and cannot
            // therefore be directly injected into the BackgroundService)
            using var scope = _serviceScopeFactory.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            int number = 0;

            while (!stoppingToken.IsCancellationRequested)
            {
                await publisher.PublishAsync(
                    new SampleMessage
                    {
                        Number = ++number
                    });

                _logger.LogInformation($"Produced {number}");

                await Task.Delay(100, stoppingToken);
            }
        }
    }
}
