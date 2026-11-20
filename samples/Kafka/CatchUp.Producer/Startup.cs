using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;

namespace Silverback.Samples.Kafka.CatchUp.Producer;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Enable Silverback
        services.AddSilverback()

            // Use Apache Kafka as message broker
            .WithConnectionToMessageBroker(options => options.AddKafka())

            // Delegate the broker clients configuration to a separate class
            .AddBrokerClientsConfigurator<BrokerClientsConfigurator>();

        // Add the hosted service that produces a bunch of price changes and stops the application
        services.AddHostedService<ProducerBackgroundService>();
    }

    public void Configure()
    {
    }
}
