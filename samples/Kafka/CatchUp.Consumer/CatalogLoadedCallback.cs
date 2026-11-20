using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Callbacks;

namespace Silverback.Samples.Kafka.CatchUp.Consumer;

public class CatalogLoadedCallback : IKafkaConsumerCaughtUpCallback
{
    private readonly ProductCatalog _catalog;

    private readonly ILogger<CatalogLoadedCallback> _logger;

    public CatalogLoadedCallback(ProductCatalog catalog, ILogger<CatalogLoadedCallback> logger)
    {
        _catalog = catalog;
        _logger = logger;
    }

    // Invoked once all the messages written to the topic until now are processed
    public Task OnConsumerCaughtUpAsync(IKafkaConsumer consumer)
    {
        _logger.LogInformation(
            "Catalog loaded by {ConsumerName}: {ProductsCount} products",
            consumer.Name,
            _catalog.Count);

        return Task.CompletedTask;
    }
}
