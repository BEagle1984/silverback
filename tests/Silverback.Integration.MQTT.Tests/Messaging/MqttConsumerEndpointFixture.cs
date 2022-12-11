// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Configuration.Mqtt;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging;

public class MqttConsumerEndpointFixture
{
    [Fact]
    public void RawName_ShouldReturnTopicName()
    {
        MqttConsumerEndpoint endpoint = new("topic", new MqttConsumerEndpointConfiguration());

        endpoint.RawName.Should().Be("topic");
    }
}
