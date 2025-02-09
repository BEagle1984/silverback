// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Messaging.Configuration.Mqtt;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging;

public class MqttProducerEndpointFixture
{
    [Fact]
    public void RawName_ShouldReturnTopicName()
    {
        MqttProducerEndpoint endpoint = new("topic", new MqttProducerEndpointConfiguration());

        endpoint.RawName.ShouldBe("topic");
    }
}
