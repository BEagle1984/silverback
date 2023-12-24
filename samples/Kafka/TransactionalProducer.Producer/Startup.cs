using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;

namespace Silverback.Samples.Kafka.TransactionalProducer.Producer;

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

        // Add the hosted service that produces the random sample messages
        services.AddHostedService<ProducerBackgroundService>();
        services.AddHostedService<ProducerBackgroundService2>();
    }

    public void Configure()
    {
    }
}
