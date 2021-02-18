using Microsoft.Extensions.DependencyInjection;

namespace Silverback.Samples.Mqtt.Basic.ConsumerV3
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Enable Silverback
            services
                .AddSilverback()

                // Use Apache Mqtt as message broker
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMqtt())

                // Delegate the inbound/outbound endpoints configuration to a separate
                // class.
                .AddEndpointsConfigurator<EndpointsConfigurator>()

                // Register the subscribers
                .AddSingletonSubscriber<SampleMessageSubscriber>();
        }

        public void Configure()
        {
        }
    }
}
