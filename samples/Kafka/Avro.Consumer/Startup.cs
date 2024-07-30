using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;

namespace Silverback.Samples.Kafka.Avro.Consumer;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Enable Silverback
        services.AddSilverback()

            // Use Apache Kafka as message broker and the Confluent schema registry
            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .AddKafkaSchemaRegistry())

            // Delegate the broker clients configuration to a separate class
            .AddBrokerClientsConfigurator<BrokerClientsConfigurator>()

            // Register the subscribers
            .AddSingletonSubscriber<AvroMessageSubscriber>();
    }

    public void Configure()
    {
    }
}
