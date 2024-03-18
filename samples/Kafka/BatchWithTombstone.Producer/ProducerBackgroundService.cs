using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.EnrichedMessages;
using Silverback.Messaging.Publishing;
using Silverback.Samples.Kafka.BatchWithTombstone.Common;

namespace Silverback.Samples.Kafka.BatchWithTombstone.Producer;

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
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        IPublisher publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        int number = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProduceMessagesAsync(publisher, ++number * 100);

            await Task.Delay(50, stoppingToken);
        }
    }

    private async Task ProduceMessagesAsync(IPublisher publisher, int number)
    {
        try
        {
            List<MessageWithHeaders<SampleMessage>> messages = new();
            List<Tombstone<SampleMessage>> tombstones = new();

            for (int i = 0; i < 100; i++)
            {
                string messageKey = $"N{number + i}";

                if (i % 30 == 0)
                {
                    tombstones.Add(new Tombstone<SampleMessage>(messageKey));
                }
                else
                {
                    messages.Add(
                        new SampleMessage { Number = number + i }
                            .WithKafkaKey(messageKey));
                }
            }

            await publisher.PublishAsync(messages);
            await publisher.PublishAsync(tombstones);

            _logger.LogInformation("Produced {FirstNumber}-{LastNumber}", number, number + 99);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to produce {FirstNumber}-{LastNumber}", number, number + 99);
        }
    }
}
