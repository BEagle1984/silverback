using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Transactions;
using Silverback.Samples.Kafka.TransactionalProducer.Common;

namespace Silverback.Samples.Kafka.TransactionalProducer.Producer;

public class ProducerBackgroundService : BackgroundService
{
    private readonly IPublisher _publisher;

    private readonly ILogger<ProducerBackgroundService> _logger;

    public ProducerBackgroundService(
        IPublisher publisher,
        ILogger<ProducerBackgroundService> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int number = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProduceMessagesAsync(++number * 1000, stoppingToken);

            await Task.Delay(100, stoppingToken);
        }
    }

    private async Task ProduceMessagesAsync(int number, CancellationToken stoppingToken)
    {
        try
        {
            using IKafkaTransaction transaction = _publisher.InitKafkaTransaction("secondary");
            _logger.LogInformation("Transaction '{TransactionalIdSuffix}' initialized", transaction.TransactionalIdSuffix);

            await _publisher.PublishAsync(
                new SampleMessage[]
                {
                    new() { Number = number + 1 },
                    new() { Number = number + 2 },
                    new() { Number = number + 3 },
                    new() { Number = number + 4 },
                    new() { Number = number + 5 }
                },
                stoppingToken);

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
