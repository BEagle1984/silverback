using MQTTnet.Formatter;
using Silverback.Messaging.Configuration;
using Silverback.Samples.Mqtt.Basic.Common;

namespace Silverback.Samples.Mqtt.Basic.ConsumerV3;

public class BrokerClientsConfigurator : IBrokerClientsConfigurator
{
    public void Configure(BrokerClientsConfigurationBuilder builder)
    {
        builder
            .AddMqttClients(
                clients => clients

                    // Configure connection
                    .ConnectViaTcp("localhost")
                    .UseProtocolVersion(MqttProtocolVersion.V310)

                    // Add an MQTT client
                    .AddClient(
                        client => client
                            .WithClientId("samples.basic.consumer-v3")

                            // Send last will message if disconnecting ungracefully
                            .SendLastWillMessage<TestamentMessage>(
                                lastWill => lastWill
                                    .SendMessage(new TestamentMessage())
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
