using Confluent.Kafka;
using Silverback.Messaging.Configuration;

namespace Silverback.Samples.Kafka.Basic.Consumer
{
    public class EndpointsConfigurator : IEndpointsConfigurator
    {
        public void Configure(IEndpointsConfigurationBuilder builder)
        {
            builder
                .AddKafkaEndpoints(endpoints => endpoints

                    // Configure the properties needed by all consumers/producers
                    .Configure(
                        config =>
                        {
                            // The bootstrap server address is needed to connect
                            config.BootstrapServers =
                                "PLAINTEXT://localhost:9092";
                        })

                    // Consume the samples-basic topic
                    .AddInbound(
                        endpoint => endpoint
                            .ConsumeFrom("samples-basic")
                            .Configure(
                                config =>
                                {
                                    // The consumer needs at least the bootstrap server address
                                    // and a group id to be able to connect
                                    config.GroupId = "sample-consumer";

                                    // AutoOffsetReset.Earliest means that the consumer must start
                                    // consuming from the beginning of the topic, if no offset was
                                    // stored for this consumer group
                                    config.AutoOffsetReset = AutoOffsetReset.Earliest;
                                })));
        }
    }
}
