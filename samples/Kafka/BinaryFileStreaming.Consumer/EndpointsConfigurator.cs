// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;
using Silverback.Messaging;
using Silverback.Messaging.BinaryFiles;
using Silverback.Messaging.Configuration;
using Silverback.Samples.BinaryFileStreaming.Consumer.Messages;

namespace Silverback.Samples.BinaryFileStreaming.Consumer
{
    public class EndpointsConfigurator : IEndpointsConfigurator
    {
        public void Configure(IEndpointsConfigurationBuilder builder)
        {
            // Consume the samples-binary-file-streaming topic
            builder.AddInbound(
                new KafkaConsumerEndpoint("samples-binary-file-streaming")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        // The consumer needs at least the bootstrap server address and a group id to be able to connect
                        BootstrapServers = "PLAINTEXT://localhost:9092",
                        GroupId = GetType().Assembly.FullName ?? "sample-consumer",

                        // AutoOffsetReset.Earliest means that the consumer must start consuming from the beginning of
                        // the topic, if no offset was stored for this consumer group
                        AutoOffsetReset = AutoOffsetReset.Earliest
                    },

                    // Force the consumer to use the BinaryFileMessageSerializer: this is not strictly necessary when
                    // the messages are produced by Silverback but it increases the interoperability, since it doesn't
                    // have to rely on the 'x-message-type' header value to switch to the BinaryFileMessageSerializer.
                    //
                    // In this example the BinaryFileMessageSerializer is also set to return a CustomBinaryFileMessage
                    // instead of the normal BinaryFileMessage. This is only needed because we want to read the custom
                    // 'x-message-filename' header, otherwise 'Serializer = new BinaryFileMessageSerializer()' would
                    // work perfectly fine (returning a basic BinaryFileMessage, without the extra properties).
                    Serializer = new BinaryFileMessageSerializer<CustomBinaryFileMessage>(),

                    // Retry each chunks sequence 5 times in case of an exception
                    ErrorPolicy = ErrorPolicy.Retry(5)
                });
        }
    }
}
