using MQTTnet.Formatter;
using Silverback.Messaging.Configuration;
using Silverback.Samples.Mqtt.Basic.Common;

namespace Silverback.Samples.Mqtt.Basic.ProducerV3;

public abstract class BrokerClientsConfigurator : IBrokerClientsConfigurator
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
                            .WithClientId("samples.basic.producer-v3")

                            // Produce the SampleMessage to the samples/basic topic
                            .Produce<SampleMessage>(
                                endpoint => endpoint
                                    .ProduceTo("samples/basic")
                                    .WithAtLeastOnceQoS()
                                    .Retain())));
    }
}
