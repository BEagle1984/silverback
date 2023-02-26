using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;

namespace Silverback.Samples.Mqtt.Basic.ProducerV3;

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
            .AddBrokerClientsConfigurator<BrokerClientsConfigurator>();

        // Add the hosted service that produces the random sample messages
        services.AddHostedService<ProducerBackgroundService>();
    }

    public void Configure()
    {
    }
}
