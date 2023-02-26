using Silverback.Messaging.Configuration;
using Silverback.Samples.Mqtt.BinaryFileStreaming.Consumer.Messages;

namespace Silverback.Samples.Mqtt.BinaryFileStreaming.Consumer;

public class BrokerClientsConfigurator : IBrokerClientsConfigurator
{
    public void Configure(BrokerClientsConfigurationBuilder builder)
    {
        builder
            .AddMqttClients(
                clients => clients

                    // Configure connection
                    .ConnectTo("localhost")

                    // Add an MQTT client
                    .AddClient(
                        client => client
                            .WithClientId("samples.binary-file.consumer")

                            // Consume the binary files from the samples/binary-files topic.
                            //
                            // A CustomBinaryMessage is used instead of the built-in
                            // BinaryMessage to be able to add the extra header
                            // containing the file name.
                            //
                            // Since the CustomBinaryMessage extends the built-in
                            // BinaryMessage, the serializer will be automatically
                            // switched to the BinaryMessageSerializer.
                            .Consume<CustomBinaryFileMessage>(
                                endpoint => endpoint
                                    .ConsumeFrom("samples/binary-files")
                                    .WithAtLeastOnceQoS()

                                    // Retry each chunks sequence 5 times in case of an
                                    // exception
                                    .OnError(policy => policy.Retry(5)))));
    }
}
