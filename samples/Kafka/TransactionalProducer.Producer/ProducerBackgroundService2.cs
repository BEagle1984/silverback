using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Transactions;
using Silverback.Samples.Kafka.TransactionalProducer.Common;

namespace Silverback.Samples.Kafka.TransactionalProducer.Producer;

public class ProducerBackgroundService2 : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    private readonly ILogger<ProducerBackgroundService2> _logger;

    public ProducerBackgroundService2(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<ProducerBackgroundService2> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Create a service scope and resolve the IPublisher
        // (the IPublisher cannot be resolved from the root scope and cannot
        // therefore be directly injected into the BackgroundService)
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IPublisher publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        int number = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProduceMessagesAsync(publisher, ++number * 100, stoppingToken);

            await Task.Delay(100, stoppingToken);
        }
    }

    private async Task ProduceMessagesAsync(IPublisher publisher, int number, CancellationToken stoppingToken)
    {
        try
        {
            using IKafkaTransaction transaction = publisher.InitKafkaTransaction("secondary");
            _logger.LogInformation("Transaction '{TransactionalIdSuffix}' initialized", transaction.TransactionalIdSuffix);

            await publisher.PublishAsync(
                new SampleMessage[]
                {
                    new() { Number = number + 1 },
                    new() { Number = number + 2 },
                    new() { Number = number + 3 },
                    new() { Number = number + 4 },
                    new() { Number = number + 5 }
                });

            _logger.LogInformation("Produced {Number} in transaction '{TransactionalIdSuffix}'", 5, transaction.TransactionalIdSuffix);
            await Task.Delay(3000, stoppingToken);

            if (number % 3000 == 0)
                throw new InvalidOperationException("Test!");

            transaction.Commit();
            _logger.LogInformation("Transaction '{TransactionalIdSuffix}' committed", transaction.TransactionalIdSuffix);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to produce {Number}", number);
        }
    }
}
