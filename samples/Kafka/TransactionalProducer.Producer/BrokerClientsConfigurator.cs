using Silverback.Messaging.Configuration;
using Silverback.Samples.Kafka.TransactionalProducer.Common;

namespace Silverback.Samples.Kafka.TransactionalProducer.Producer;

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

                            // Enable transactions and define the transactional id
                            .EnableTransactions("sample")

                            // Produce the SampleMessage to the samples-transactional-producer topic
                            .Produce<SampleMessage>(
                                endpoint => endpoint
                                    .ProduceTo("samples-transactional-producer"))));
    }
}
