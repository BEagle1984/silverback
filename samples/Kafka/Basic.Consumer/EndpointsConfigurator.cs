using Confluent.Kafka;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;

namespace Silverback.Samples.Kafka.Basic.Consumer
{
    public class EndpointsConfigurator : IEndpointsConfigurator
    {
        public void Configure(IEndpointsConfigurationBuilder builder)
        {
            // Consume the samples-basic topic
            builder.AddInbound(
                new KafkaConsumerEndpoint("samples-basic")
                {
                    Configuration = new KafkaConsumerConfig
                    {
                        // The consumer needs at least the bootstrap server address
                        // and a group id to be able to connect
                        BootstrapServers = "PLAINTEXT://localhost:9092",
                        GroupId = "sample-consumer",

                        // AutoOffsetReset.Earliest means that the consumer must start
                        // consuming from the beginning of the topic, if no offset was
                        // stored for this consumer group
                        AutoOffsetReset = AutoOffsetReset.Earliest
                    }
                });
        }
    }
}
