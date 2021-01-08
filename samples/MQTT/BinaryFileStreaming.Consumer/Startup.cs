using Microsoft.Extensions.DependencyInjection;
using Silverback.Samples.Mqtt.BinaryFileStreaming.Consumer.Subscribers;

namespace Silverback.Samples.Mqtt.BinaryFileStreaming.Consumer
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
                .AddSingletonSubscriber<BinaryFileSubscriber>();
        }

        public void Configure()
        {
        }
    }
}
