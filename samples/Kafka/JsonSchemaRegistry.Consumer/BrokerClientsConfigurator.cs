using Silverback.Messaging.Configuration;
using Silverback.Samples.Kafka.JsonSchemaRegistry.Common;

namespace Silverback.Samples.Kafka.JsonSchemaRegistry.Consumer;

public class BrokerClientsConfigurator : IBrokerClientsConfigurator
{
    public void Configure(BrokerClientsConfigurationBuilder builder)
    {
        builder
            .AddKafkaClients(
                clients => clients

                    // The bootstrap server address is needed to connect
                    .WithBootstrapServers("PLAINTEXT://localhost:19092")

                    // Add a consumer
                    .AddConsumer(
                        consumer => consumer

                            // Set the consumer group id
                            .WithGroupId("sample-consumer")

                            // AutoOffsetReset.Earliest means that the consumer
                            // must start consuming from the beginning of the topic,
                            // if no offset was stored for this consumer group
                            .AutoResetOffsetToEarliest()

                            // Consume the AvroMessage from the samples-avro topic
                            .Consume<SampleMessage>(
                                endpoint => endpoint
                                    .ConsumeFrom("samples-json-schema-registry")

                                    // Configure JSON deserialization
                                    .DeserializeJsonUsingSchemaRegistry(
                                        json => json
                                            .ConnectToSchemaRegistry("localhost:8081")))));
    }
}
