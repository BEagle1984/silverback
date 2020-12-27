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

                                // Force producing to a specific partition (0 in this
                                // case) to be able to scale to multiple producers
                                // writing to the same topic. Assigning a different
                                // partition to each one will ensure that the chunks
                                // are always contiguous.
                                // This isn't mandatory and necessary only when
                                // horizontally scaling the producer.
                                // (In the final solution the "0" constant value
                                // should be replaced by a configuration setting.)
                                .ProduceTo("samples-binary-file-streaming", 0)

                                // Split the binary files into chunks of 512 kB
                                .EnableChunking(524288)));
        }
    }
}
