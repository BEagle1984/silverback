using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silverback.Messaging.Publishing;
using Silverback.Samples.Kafka.Basic.Common;

namespace Silverback.Samples.Kafka.Basic.Producer
{
    public class ProducerBackgroundService : BackgroundService
    {
        private readonly Random _random = new Random((int)DateTime.Now.Ticks);

        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ProducerBackgroundService(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Create a service scope and resolve the IPublisher
            // (the IPublisher cannot be resolved from the root scope and cannot
            // therefore be directly injected into the BackgroundService)
            using var scope = _serviceScopeFactory.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            while (!stoppingToken.IsCancellationRequested)
            {
                await publisher.PublishAsync(
                    new SampleMessage
                    {
                        SomeRandomNumber = _random.Next(int.MaxValue)
                    });

                await Task.Delay(100, stoppingToken);
            }
        }
    }
}
