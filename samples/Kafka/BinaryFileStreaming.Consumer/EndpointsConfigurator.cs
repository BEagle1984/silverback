using Confluent.Kafka;
using Silverback.Messaging.Configuration;
using Silverback.Samples.Kafka.BinaryFileStreaming.Consumer.Messages;

namespace Silverback.Samples.Kafka.BinaryFileStreaming.Consumer
{
    public class EndpointsConfigurator : IEndpointsConfigurator
    {
        public void Configure(IEndpointsConfigurationBuilder builder)
        {
            builder
                .AddKafkaEndpoints(
                    endpoints => endpoints

                        // Configure the properties needed by all consumers/producers
                        .Configure(
                            config =>
                            {
                                // The bootstrap server address is needed to connect
                                config.BootstrapServers =
                                    "PLAINTEXT://localhost:9092";
                            })

                        // Consume the samples-binary-file-streaming topic
                        .AddInbound(
                            endpoint => endpoint

                                // Manually assign the partitions to prevent the
                                // broker to rebalance in the middle of a potentially
                                // huge sequence of chunks. This is just an
                                // optimization and isn't strictly necessary.
                                // (The partitions resolver function returns the
                                // untouched collection to assign all available
                                // partitions.)
                                .ConsumeFrom(
                                    "samples-binary-file-streaming",
                                    partitions => partitions)
                                .Configure(
                                    config =>
                                    {
                                        // The consumer needs at least the bootstrap
                                        // server address and a group id to be able
                                        // to connect
                                        config.GroupId = "sample-consumer";

                                        // AutoOffsetReset.Earliest means that the
                                        // consumer must start consuming from the
                                        // beginning of the topic, if no offset was
                                        // stored for this consumer group
                                        config.AutoOffsetReset =
                                            AutoOffsetReset.Earliest;
                                    })

                                // Force the consumer to use the
                                // BinaryFileMessageSerializer: this is not strictly
                                // necessary when the messages are produced by
                                // Silverback but it increases the interoperability,
                                // since it doesn't have to rely on the
                                // 'x-message-type' header value to switch to the
                                // BinaryFileMessageSerializer.
                                //
                                // In this example the BinaryFileMessageSerializer is
                                // also set to return a CustomBinaryFileMessage
                                // instead of the normal BinaryFileMessage. This is
                                // only needed because we want to read the custom
                                // 'x-message-filename' header, otherwise
                                // 'ConsumeBinaryFiles()' would work perfectly fine
                                // (returning a basic BinaryFileMessage, without the
                                // extra properties).
                                .ConsumeBinaryFiles(
                                    serializer =>
                                        serializer
                                            .UseModel<CustomBinaryFileMessage>())

                                // Retry each chunks sequence 5 times in case of an
                                // exception
                                .OnError(policy => policy.Retry(5))));
        }
    }
}
