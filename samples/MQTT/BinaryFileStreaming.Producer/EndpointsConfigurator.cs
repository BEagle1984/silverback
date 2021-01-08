using MQTTnet.Protocol;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;

namespace Silverback.Samples.Mqtt.BinaryFileStreaming.Producer
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
                                .WithClientId("samples.binary-file.producer")
                                .ConnectViaTcp("localhost"))

                        // Produce the binary files to the
                        // samples-binary-file-streaming topic
                        .AddOutbound<BinaryFileMessage>(
                            endpoint => endpoint
                                .ProduceTo("samples/binary-files")
                                .WithQualityOfServiceLevel(
                                    MqttQualityOfServiceLevel.AtLeastOnce)));
        }
    }
}
