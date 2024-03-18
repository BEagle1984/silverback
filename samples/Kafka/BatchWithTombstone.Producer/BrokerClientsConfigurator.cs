using Silverback.Messaging.Configuration;
using Silverback.Samples.Kafka.BatchWithTombstone.Common;

namespace Silverback.Samples.Kafka.BatchWithTombstone.Producer;

public class BrokerClientsConfigurator : IBrokerClientsConfigurator
{
    public void Configure(BrokerClientsConfigurationBuilder builder)
    {
        builder
            .AddKafkaClients(
                clients => clients

                    // The bootstrap server address is needed to connect
                    .WithBootstrapServers("PLAINTEXT://localhost:9092")

                    // Add a producer
                    .AddProducer(
                        producer => producer

                            // Produce the SampleMessage to the samples-batch topic
                            .Produce<SampleMessage>(
                                endpoint => endpoint
                                    .ProduceTo("samples-batch-with-tombstone"))));
    }
}
