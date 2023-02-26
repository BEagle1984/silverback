using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;

namespace Silverback.Samples.Mqtt.BinaryFileStreaming.Producer;

public abstract class BrokerClientsConfigurator : IBrokerClientsConfigurator
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
                            .WithClientId("samples.binary-file.producer")

                            // Produce the binary files to the samples/binary-files topic
                            .Produce<BinaryMessage>(
                                endpoint => endpoint
                                    .ProduceTo("samples/binary-files")
                                    .WithAtLeastOnceQoS()
                                    .Retain())));
    }
}
