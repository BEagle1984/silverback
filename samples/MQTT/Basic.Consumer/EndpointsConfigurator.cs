using MQTTnet.Protocol;
using Silverback.Messaging.Configuration;
using Silverback.Samples.Mqtt.Basic.Common;

namespace Silverback.Samples.Mqtt.Basic.Consumer
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
                                .WithClientId("samples.basic.consumer")
                                .ConnectViaTcp("localhost")

                                // Send last will message if disconnecting
                                // ungracefully
                                .SendLastWillMessage(
                                    lastWill => lastWill
                                        .Message(new TestamentMessage())
                                        .ProduceTo("samples/testaments")))

                        // Consume the samples/basic topic
                        .AddInbound(
                            endpoint => endpoint
                                .ConsumeFrom("samples/basic")
                                .WithQualityOfServiceLevel(
                                    MqttQualityOfServiceLevel.AtLeastOnce)

                                // It is mandatory to specify the message type, since
                                // MQTT doesn't support message headers
                                .DeserializeJson(
                                    serializer => serializer
                                        .UseFixedType<SampleMessage>())

                                // Silently skip the messages in case of exception
                                .OnError(policy => policy.Skip())));
        }
    }
}
