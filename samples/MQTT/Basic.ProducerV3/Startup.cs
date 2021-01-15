using Microsoft.Extensions.DependencyInjection;

namespace Silverback.Samples.Mqtt.Basic.ProducerV3
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
                .AddEndpointsConfigurator<EndpointsConfigurator>();

            // Add the hosted service that produces the random sample messages
            services.AddHostedService<ProducerBackgroundService>();
        }

        public void Configure()
        {
        }
    }
}
