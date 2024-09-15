using Silverback.Messaging.Configuration;
using Silverback.Samples.Mqtt.Basic.Common;

namespace Silverback.Samples.Mqtt.Basic.Producer;

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
                            .WithClientId("samples.basic.producer")

                            // Produce the SampleMessage to the samples/basic topic
                            .Produce<SampleMessage>(
                                endpoint => endpoint
                                    .ProduceTo("samples/basic")
                                    .WithAtLeastOnceQoS()
                                    .Retain()
                                    .IgnoreNoMatchingSubscribersError())));
    }
}
