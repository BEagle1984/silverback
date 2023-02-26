using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Samples.Kafka.BinaryFileStreaming.Consumer.Subscribers;

namespace Silverback.Samples.Kafka.BinaryFileStreaming.Consumer;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Enable Silverback
        services.AddSilverback()

            // Use Apache Kafka as message broker
            .WithConnectionToMessageBroker(options => options.AddKafka())

            // Delegate the broker clients configuration to a separate class
            .AddBrokerClientsConfigurator<BrokerClientsConfigurator>()

            // Register the subscribers
            .AddSingletonSubscriber<BinaryFileSubscriber>();
    }

    public void Configure()
    {
    }
}
