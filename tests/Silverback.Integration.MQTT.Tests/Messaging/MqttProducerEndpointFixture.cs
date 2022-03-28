// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Configuration.Mqtt;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging;

public class MqttProducerEndpointFixture
{
    [Fact]
    public void RawName_ShouldReturnTopicName()
    {
        MqttProducerEndpoint endpoint = new("topic", new MqttProducerEndpointConfiguration());

        endpoint.RawName.Should().Be("topic");
    }
}
