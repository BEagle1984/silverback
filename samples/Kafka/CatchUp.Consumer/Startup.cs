using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;

namespace Silverback.Samples.Kafka.CatchUp.Consumer;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // The in-memory state loaded from the topic
        services.AddSingleton<ProductCatalog>();

        // Enable Silverback
        services.AddSilverback()

            // Use Apache Kafka as message broker
            .WithConnectionToMessageBroker(options => options.AddKafka())

            // Delegate the broker clients configuration to a separate class
            .AddBrokerClientsConfigurator<BrokerClientsConfigurator>()

            // Register the subscriber updating the catalog
            .AddSingletonSubscriber<ProductPriceChangedSubscriber>()

            // Register the callback invoked once the consumer is caught up
            .AddSingletonBrokerClientCallback<CatalogLoadedCallback>();

        // Add the hosted service that waits until the catalog is loaded before using it
        // (registered after Silverback, so that the consumer is already created)
        services.AddHostedService<CatalogReportService>();
    }

    public void Configure()
    {
    }
}
