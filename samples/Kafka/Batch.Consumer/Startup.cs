using Microsoft.Extensions.DependencyInjection;

namespace Silverback.Samples.Kafka.Batch.Consumer
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Enable Silverback
            services
                .AddSilverback()

                // Use Apache Kafka as message broker
                .WithConnectionToMessageBroker(
                    options => options
                        .AddKafka())

                // Delegate the inbound/outbound endpoints configuration to a separate
                // class.
                .AddEndpointsConfigurator<EndpointsConfigurator>()

                // Register the subscribers
                .AddSingletonSubscriber<SampleMessageBatchSubscriber>();
        }

        public void Configure()
        {
        }
    }
}
