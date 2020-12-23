using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;

namespace Silverback.Samples.Kafka.BinaryFileStreaming.Producer
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

                        // Produce the binary files to the
                        // samples-binary-file-streaming topic
                        .AddOutbound<BinaryFileMessage>(
                            endpoint => endpoint
                                .ProduceTo("samples-binary-file-streaming")

                                // Split the binary files into chunks of 512 kB
                                .EnableChunking(524288)));
        }
    }
}
