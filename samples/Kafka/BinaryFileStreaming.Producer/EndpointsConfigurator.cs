// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences.Chunking;

namespace Silverback.Samples.BinaryFileStreaming.Producer
{
    public class EndpointsConfigurator : IEndpointsConfigurator
    {
        public void Configure(IEndpointsConfigurationBuilder builder)
        {
            builder

                // Produce the binary files to the samples-binary-file-streaming topic
                .AddOutbound<BinaryFileMessage>(
                    new KafkaProducerEndpoint("samples-binary-file-streaming")
                    {
                        // The producer only needs the bootstrap server address to be able to connect
                        Configuration = new KafkaProducerConfig
                        {
                            BootstrapServers = "PLAINTEXT://localhost:9092"
                        },

                        // Split the binary files into chunks of 512 kB
                        Chunk = new ChunkSettings
                        {
                            Size = 524288
                        }
                    });
        }
    }
}
