// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Messaging.Configuration.Mqtt;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging;

public class MqttProducerEndpointTests
{
    [Fact]
    public void RawName_ShouldReturnTopicName()
    {
        MqttProducerEndpoint endpoint = new("topic", new MqttProducerEndpointConfiguration());

        endpoint.RawName.ShouldBe("topic");
    }
}
