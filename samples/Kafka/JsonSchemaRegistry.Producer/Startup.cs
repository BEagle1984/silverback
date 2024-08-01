using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;

namespace Silverback.Samples.Kafka.JsonSchemaRegistry.Producer;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Enable Silverback
        services.AddSilverback()

            // Use Apache Kafka as message broker and the Confluent schema registry
            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .AddConfluentSchemaRegistry())

            // Delegate the broker clients configuration to a separate class
            .AddBrokerClientsConfigurator<BrokerClientsConfigurator>();

        // Add the hosted service that produces the random sample messages
        services.AddHostedService<ProducerBackgroundService>();
    }

    public void Configure()
    {
    }
}
