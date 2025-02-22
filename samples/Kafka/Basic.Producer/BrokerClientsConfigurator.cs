using Silverback.Messaging.Configuration;
using Silverback.Samples.Kafka.Basic.Common;

namespace Silverback.Samples.Kafka.Basic.Producer;

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

                            // Produce the SampleMessage to the samples-basic topic
                            .Produce<SampleMessage>(
                                endpoint => endpoint
                                    .ProduceTo("samples-basic"))));
    }
}
