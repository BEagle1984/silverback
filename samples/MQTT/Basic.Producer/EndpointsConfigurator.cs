﻿using MQTTnet.Protocol;
using Silverback.Messaging.Configuration;
using Silverback.Samples.Mqtt.Basic.Common;

namespace Silverback.Samples.Mqtt.Basic.Producer
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
                                .WithClientId("samples.basic.producer")
                                .ConnectViaTcp("localhost"))

                        // Produce the SampleMessage to the samples-basic topic
                        .AddOutbound<SampleMessage>(
                            endpoint => endpoint
                                .ProduceTo("samples/basic")
                                .WithQualityOfServiceLevel(
                                    MqttQualityOfServiceLevel.AtLeastOnce)
                                .Retain()));
        }
    }
}
