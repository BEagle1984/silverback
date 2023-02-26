using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;

namespace Silverback.Samples.Mqtt.Basic.ConsumerV3;

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
            .AddSingletonSubscriber<SampleMessageSubscriber>();
    }

    public void Configure()
    {
    }
}
