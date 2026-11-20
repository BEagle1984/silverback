using Silverback.Messaging.Configuration;
using Silverback.Samples.Kafka.CatchUp.Common;

namespace Silverback.Samples.Kafka.CatchUp.Producer;

public class BrokerClientsConfigurator : IBrokerClientsConfigurator
{
    public void Configure(BrokerClientsConfigurationBuilder builder)
    {
        builder
            .AddKafkaClients(
                clients => clients

                    // The bootstrap server address is needed to connect
                    .WithBootstrapServers("PLAINTEXT://localhost:19092")

                    // Add a producer
                    .AddProducer(
                        producer => producer

                            // Produce the ProductPriceChanged to the samples-catch-up topic
                            .Produce<ProductPriceChanged>(
                                endpoint => endpoint
                                    .ProduceTo("samples-catch-up")

                                    // Use the product id as message key, so that all the
                                    // changes of a product are written to the same partition
                                    .SetKafkaKey(message => message?.ProductId))));
    }
}
