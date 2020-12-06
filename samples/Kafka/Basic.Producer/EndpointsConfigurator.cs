using Silverback.Messaging.Configuration;
using Silverback.Samples.Kafka.Basic.Common;

namespace Silverback.Samples.Kafka.Basic.Producer
{
    public class EndpointsConfigurator : IEndpointsConfigurator
    {
        public void Configure(IEndpointsConfigurationBuilder builder)
        {
            builder
                .AddKafkaEndpoints(
                    endpoints => endpoints

                        // Configure the properties needed by all consumers/producers
                        .Configure(
                            config =>
                            {
                                // The bootstrap server address is needed to connect
                                config.BootstrapServers =
                                    "PLAINTEXT://localhost:9092";
                            })

                        // Produce the SampleMessage to the samples-basic topic
                        .AddOutbound<SampleMessage>(
                            endpoint => endpoint
                                .ProduceTo("samples-basic")));
        }
    }
}
