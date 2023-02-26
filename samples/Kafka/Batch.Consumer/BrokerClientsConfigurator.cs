using System;
using Silverback.Messaging.Configuration;
using Silverback.Samples.Kafka.Batch.Common;

namespace Silverback.Samples.Kafka.Batch.Consumer;

public class BrokerClientsConfigurator : IBrokerClientsConfigurator
{
    public void Configure(BrokerClientsConfigurationBuilder builder)
    {
        builder
            .AddKafkaClients(
                clients => clients

                    // The bootstrap server address is needed to connect
                    .WithBootstrapServers("PLAINTEXT://localhost:9092")

                    // Add a consumer
                    .AddConsumer(
                        consumer => consumer

                            // Set the consumer group id
                            .WithGroupId("sample-consumer")

                            // AutoOffsetReset.Earliest means that the consumer
                            // must start consuming from the beginning of the topic,
                            // if no offset was stored for this consumer group
                            .AutoResetOffsetToEarliest()

                            // Consume the SampleMessage from the samples-batch topic
                            .Consume<SampleMessage>(
                                endpoint => endpoint
                                    .ConsumeFrom("samples-batch")

                                    // Configure processing in batches of 100 messages,
                                    // with a max wait time of 5 seconds
                                    .EnableBatchProcessing(100, TimeSpan.FromSeconds(5)))));
    }
}
