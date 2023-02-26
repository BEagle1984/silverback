using Silverback.Messaging.Configuration;
using Silverback.Samples.Mqtt.Basic.Common;

namespace Silverback.Samples.Mqtt.Basic.Consumer;

public class BrokerClientsConfigurator : IBrokerClientsConfigurator
{
    public void Configure(BrokerClientsConfigurationBuilder builder)
    {
        builder
            .AddMqttClients(
                clients => clients

                    // Configure connection
                    .ConnectViaTcp("localhost")

                    // Add an MQTT client
                    .AddClient(
                        client => client
                            .WithClientId("samples.basic.consumer")

                            // Send last will message if disconnecting ungracefully
                            .SendLastWillMessage<TestamentMessage>(
                                lastWill => lastWill
                                    .Message(new TestamentMessage())
                                    .ProduceTo("samples/testaments"))

                            // Consume the samples/basic topic
                            .Consume(
                                endpoint => endpoint
                                    .ConsumeFrom("samples/basic")
                                    .WithAtLeastOnceQoS()

                                    // Silently skip the messages in case of exception
                                    .OnError(policy => policy.Skip()))));
    }
}
