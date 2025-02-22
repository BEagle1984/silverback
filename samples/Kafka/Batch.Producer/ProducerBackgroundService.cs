using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Samples.Kafka.Batch.Common;

namespace Silverback.Samples.Kafka.Batch.Producer;

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
            await ProduceMessagesAsync(++number + 100);

            await Task.Delay(50, stoppingToken);
        }
    }

    private async Task ProduceMessagesAsync(int number)
    {
        try
        {
            List<SampleMessage> messages = [];
            for (int i = 0; i < 100; i++)
            {
                messages.Add(new SampleMessage { Number = number + i });
            }

            await _publisher.WrapAndPublishBatchAsync(
                messages,
                envelope => envelope.SetKafkaKey($"N{envelope.Message?.Number}"));

            _logger.LogInformation("Produced {FirstNumber}-{LastNumber}", number, number + 99);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to produce {FirstNumber}-{LastNumber}", number, number + 99);
        }
    }
}
