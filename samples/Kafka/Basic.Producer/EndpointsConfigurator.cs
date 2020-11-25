using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Samples.Kafka.Basic.Common;

namespace Silverback.Samples.Kafka.Basic.Producer
{
    public class EndpointsConfigurator : IEndpointsConfigurator
    {
        public void Configure(IEndpointsConfigurationBuilder builder)
        {
            builder

                // Produce the messages to the samples-basic topic
                .AddOutbound<SampleMessage>(
                    new KafkaProducerEndpoint("samples-basic")
                    {
                        // The producer only needs the bootstrap server address to be
                        // able to connect
                        Configuration = new KafkaProducerConfig
                        {
                            BootstrapServers = "PLAINTEXT://localhost:9092"
                        }
                    });
        }
    }
}
