using Silverback.Messaging.Configuration;
using Silverback.Samples.Kafka.Batch.Common;

namespace Silverback.Samples.Kafka.Batch.Producer;

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

                            // Produce the SampleMessage to the samples-batch topic
                            .Produce<SampleMessage>(
                                endpoint => endpoint
                                    .ProduceTo("samples-batch"))));
    }
}
