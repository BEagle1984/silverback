using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;

namespace Silverback.Samples.Kafka.CatchUp.Consumer;

public class CatalogReportService : BackgroundService
{
    private readonly IConsumerCollection _consumers;

    private readonly ProductCatalog _catalog;

    private readonly ILogger<CatalogReportService> _logger;

    public CatalogReportService(
        IConsumerCollection consumers,
        ProductCatalog catalog,
        ILogger<CatalogReportService> logger)
    {
        _consumers = consumers;
        _catalog = catalog;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IKafkaConsumer consumer = (IKafkaConsumer)_consumers[BrokerClientsConfigurator.ConsumerName];

        // Don't use the catalog until it's loaded
        _logger.LogInformation("Waiting for the catalog to be loaded...");
        await consumer.WaitUntilCaughtUpAsync(stoppingToken);

        // The consumer keeps the catalog up to date with the new price changes
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "{ProductsCount} products in the catalog, product-01 costs {Price}",
                _catalog.Count,
                _catalog.GetPrice("product-01"));

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
