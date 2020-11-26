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
                                .ConnectViaTcp("localhost")

                                // Send last will message if disconnecting
                                // ungracefully
                                .SendLastWillMessage(
                                    lastWill => lastWill
                                        .Message(new TestamentMessage())
                                        .ProduceTo("samples/testaments")))

                        // Consume the SampleMessage from the samples-basic topic
                        // Note: It is mandatory to specify the message type, since
                        //       MQTT doesn't support message headers
                        .AddInbound(
                            endpoint => endpoint
                                .ConsumeFrom("samples/basic")
                                .DeserializeJson(
                                    serializer => serializer
                                        .UseFixedType<SampleMessage>())));
        }
    }
}
