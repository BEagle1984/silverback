using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Publishing;
using Silverback.Samples.Mqtt.Basic.Common;

namespace Silverback.Samples.Mqtt.Basic.ProducerV3;

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
            await ProduceMessageAsync(++number);

            await Task.Delay(100, stoppingToken);
        }
    }

    private async Task ProduceMessageAsync(int number)
    {
        try
        {
            await _publisher.PublishAsync(
                new SampleMessage
                {
                    Number = number
                });

            _logger.LogInformation("Produced {Number}", number);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to produce {Number}", number);
        }
    }
}
