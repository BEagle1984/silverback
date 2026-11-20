using Silverback.Messaging.Configuration;
using Silverback.Samples.Kafka.CatchUp.Common;

namespace Silverback.Samples.Kafka.CatchUp.Consumer;

public class BrokerClientsConfigurator : IBrokerClientsConfigurator
{
    public const string ConsumerName = "catalog-loader";

    public void Configure(BrokerClientsConfigurationBuilder builder)
    {
        builder
            .AddKafkaClients(
                clients => clients

                    // The bootstrap server address is needed to connect
                    .WithBootstrapServers("PLAINTEXT://localhost:19092")

                    // Add a consumer, named to retrieve it from the IConsumerCollection
                    .AddConsumer(
                        ConsumerName,
                        consumer => consumer

                            // Notify once all the partitions are consumed until their end,
                            // then keep consuming to keep the catalog up to date
                            .NotifyWhenCaughtUp()

                            // The whole topic is consumed at every start, so there's no need
                            // to commit the offsets (nor to join a consumer group)
                            .DisableOffsetsCommit()

                            // Consume the ProductPriceChanged from the samples-catch-up topic
                            .Consume<ProductPriceChanged>(
                                endpoint => endpoint

                                    // Assign all the partitions of the topic and consume
                                    // them from the beginning
                                    .ConsumeFromBeginning("samples-catch-up"))));
    }
}
