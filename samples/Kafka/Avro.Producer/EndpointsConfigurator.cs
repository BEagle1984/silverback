using Silverback.Examples.Messages;
using Silverback.Messaging.Configuration;

namespace Silverback.Samples.Kafka.Avro.Producer
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

                        // Produce the AvroMessage to the samples-avro topic
                        .AddOutbound<AvroMessage>(
                            endpoint => endpoint
                                .ProduceTo("samples-avro")

                                // Configure Avro serialization
                                .SerializeAsAvro(
                                    avro => avro.Configure(
                                        schemaRegistry =>
                                        {
                                            schemaRegistry.Url = "localhost:8081";
                                        },
                                        serializer =>
                                        {
                                            serializer.AutoRegisterSchemas = true;
                                        }))));
        }
    }
}
