using Silverback.Examples.Messages;
using Silverback.Messaging.Configuration;

namespace Silverback.Samples.Kafka.Avro.Producer;

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

                            // Produce the AvroMessage to the samples-avro topic
                            .Produce<AvroMessage>(
                                endpoint => endpoint
                                    .ProduceTo("samples-avro")

                                    // Configure Avro serialization
                                    .SerializeAsAvro(
                                        avro => avro.Configure(
                                            schemaRegistry =>
                                            {
                                                schemaRegistry.Url =
                                                    "localhost:8081";
                                            },
                                            serializer =>
                                            {
                                                serializer.AutoRegisterSchemas =
                                                    true;
                                            })))));
    }
}
