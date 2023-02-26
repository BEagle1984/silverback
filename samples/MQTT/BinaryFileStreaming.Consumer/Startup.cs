using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Samples.Mqtt.BinaryFileStreaming.Consumer.Subscribers;

namespace Silverback.Samples.Mqtt.BinaryFileStreaming.Consumer;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Enable Silverback
        services.AddSilverback()

            // Use Apache Mqtt as message broker
            .WithConnectionToMessageBroker(
                options => options
                    .AddMqtt())

            // Delegate the broker clients configuration to a separate class
            .AddBrokerClientsConfigurator<BrokerClientsConfigurator>()

            // Register the subscribers
            .AddSingletonSubscriber<BinaryFileSubscriber>();
    }

    public void Configure()
    {
    }
}
