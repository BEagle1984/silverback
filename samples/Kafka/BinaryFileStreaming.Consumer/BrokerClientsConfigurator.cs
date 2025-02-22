using Silverback.Messaging.Configuration;
using Silverback.Samples.Kafka.BinaryFileStreaming.Consumer.Messages;

namespace Silverback.Samples.Kafka.BinaryFileStreaming.Consumer;

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

                            // Consume the CustomBinaryMessage from the
                            // samples-binary-file-streaming topic.
                            //
                            // A CustomBinaryMessage is used instead of the built-in
                            // BinaryMessage to be able to add the extra header
                            // containing the file name.
                            //
                            // Since the CustomBinaryMessage extends the built-in
                            // BinaryMessage, the serializer will be automatically
                            // switched to the BinaryMessageSerializer.
                            .Consume<CustomBinaryMessage>(
                                endpoint => endpoint

                                    // Manually assign the partitions to prevent the
                                    // broker to rebalance in the middle of a
                                    // potentially huge sequence of chunks. This is
                                    // just an optimization and isn't strictly
                                    // necessary.
                                    // (The partitions resolver function returns the
                                    // untouched collection to assign all available
                                    // partitions.)
                                    .ConsumeFrom(
                                        "samples-binary-file-streaming",
                                        partitions => partitions)

                                    // Retry each chunks sequence 5 times in case of an
                                    // exception
                                    .OnError(policy => policy.Retry(5)))));
    }
}
