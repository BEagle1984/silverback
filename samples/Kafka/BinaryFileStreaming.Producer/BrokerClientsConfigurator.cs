using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;

namespace Silverback.Samples.Kafka.BinaryFileStreaming.Producer;

public class BrokerClientsConfigurator : IBrokerClientsConfigurator
{
    public void Configure(BrokerClientsConfigurationBuilder builder)
    {
        builder
            .AddKafkaClients(
                clients => clients

                    // The bootstrap server address is needed to connect
                    .WithBootstrapServers("PLAINTEXT://localhost:19092")

                    // Add a producer
                    .AddProducer(
                        producer => producer

                            // Produce the binary files to the
                            // samples-binary-file-streaming topic.
                            //
                            // Force producing to a specific partition (0 in this
                            // case) to be able to scale to multiple producers
                            // writing to the same topic. Assigning a different
                            // partition to each one will ensure that the chunks
                            // are always contiguous.
                            // This isn't mandatory and necessary only when
                            // horizontally scaling the producer.
                            // (In the final solution the "0" constant value
                            // should be replaced by a configuration setting.)
                            .Produce<BinaryMessage>(
                                endpoint => endpoint
                                    .ProduceTo("samples-binary-file-streaming", 0)

                                    // Split the binary files into chunks of 512 kB
                                    .EnableChunking(524288))));
    }
}
