// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Messaging.Configuration.Mqtt;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging;

public class MqttConsumerEndpointFixture
{
    [Fact]
    public void RawName_ShouldReturnTopicName()
    {
        MqttConsumerEndpoint endpoint = new("topic", new MqttConsumerEndpointConfiguration());

        endpoint.RawName.ShouldBe("topic");
    }
}
