using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Publishing;
using Silverback.Samples.Kafka.CatchUp.Common;

namespace Silverback.Samples.Kafka.CatchUp.Producer;

public class ProducerBackgroundService : BackgroundService
{
    private const int ProductsCount = 20;

    private const int MessagesCount = 1000;

    private readonly IPublisher _publisher;

    private readonly IHostApplicationLifetime _applicationLifetime;

    private readonly ILogger<ProducerBackgroundService> _logger;

    public ProducerBackgroundService(
        IPublisher publisher,
        IHostApplicationLifetime applicationLifetime,
        ILogger<ProducerBackgroundService> logger)
    {
        _publisher = publisher;
        _applicationLifetime = applicationLifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int producedCount = 0;

        // Produce a bunch of price changes
        for (int i = 1; i <= MessagesCount && !stoppingToken.IsCancellationRequested; i++)
        {
            // Change the price of a random product
            string productId = $"product-{Random.Shared.Next(1, ProductsCount + 1):00}";
            decimal price = Random.Shared.Next(100, 10000) / 100m;

            if (await TryProduceMessageAsync(productId, price))
                producedCount++;

            if (i % 100 == 0)
                _logger.LogInformation("Produced {Count} price changes", producedCount);
        }

        _logger.LogInformation("Done: {Count} price changes produced", producedCount);

        // Then stop the application
        _applicationLifetime.StopApplication();
    }

    private async Task<bool> TryProduceMessageAsync(string productId, decimal price)
    {
        try
        {
            await _publisher.PublishAsync(
                new ProductPriceChanged
                {
                    ProductId = productId,
                    Price = price
                });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to produce {ProductId}", productId);
            return false;
        }
    }
}
