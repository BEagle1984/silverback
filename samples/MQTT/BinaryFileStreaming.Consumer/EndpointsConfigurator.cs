using MQTTnet.Protocol;
using Silverback.Messaging.Configuration;
using Silverback.Samples.Mqtt.BinaryFileStreaming.Consumer.Messages;

namespace Silverback.Samples.Mqtt.BinaryFileStreaming.Consumer
{
    public class EndpointsConfigurator : IEndpointsConfigurator
    {
        public void Configure(IEndpointsConfigurationBuilder builder)
        {
            builder
                .AddMqttEndpoints(
                    endpoints => endpoints

                        // Configure the client options
                        .Configure(
                            config => config
                                .WithClientId("samples.binary-file.consumer")
                                .ConnectViaTcp("localhost"))

                        // Consume the samples-binary-file-streaming topic
                        .AddInbound(
                            endpoint => endpoint
                                .ConsumeFrom("samples/binary-files")
                                .WithQualityOfServiceLevel(
                                    MqttQualityOfServiceLevel.AtLeastOnce)

                                // Force the consumer to use the
                                // BinaryFileMessageSerializer: this is not strictly
                                // necessary when the messages are produced by
                                // Silverback but it increases the interoperability,
                                // since it doesn't have to rely on the
                                // 'x-message-type' header value to switch to the
                                // BinaryFileMessageSerializer.
                                //
                                // In this example the BinaryFileMessageSerializer is
                                // also set to return a CustomBinaryFileMessage
                                // instead of the normal BinaryFileMessage. This is
                                // only needed because we want to read the custom
                                // 'x-message-filename' header, otherwise
                                // 'ConsumeBinaryFiles()' would work perfectly fine
                                // (returning a basic BinaryFileMessage, without the
                                // extra properties).
                                .ConsumeBinaryFiles(
                                    serializer =>
                                        serializer
                                            .UseModel<CustomBinaryFileMessage>())

                                // Retry each chunks sequence 5 times in case of an
                                // exception
                                .OnError(policy => policy.Retry(5))));
        }
    }
}
