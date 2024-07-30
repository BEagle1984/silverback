using Silverback.Messaging.Configuration;
using Silverback.Samples.Kafka.JsonSchemaRegistry.Common;

namespace Silverback.Samples.Kafka.JsonSchemaRegistry.Producer;

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
                            .Produce<SampleMessage>(
                                endpoint => endpoint
                                    .ProduceTo("samples-json-schema-registry")

                                    // Configure JSON serialization
                                    .SerializeAsJsonUsingSchemaRegistry(
                                        avro => avro
                                            .ConnectToSchemaRegistry("localhost:8081")
                                            .Configure(
                                                serializer =>
                                                {
                                                    serializer.AutoRegisterSchemas = false;
                                                    serializer.UseLatestVersion = true;
                                                })))));
    }
}
