// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using MQTTnet.Packets;
using Shouldly;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Messages;

public class MqttHeadersMappingExtensionsTests
{
    [Fact]
    public void ToUserProperties_ShouldMapHeaders()
    {
        MessageHeaderCollection headers = new()
        {
            { "one", "1" },
            { "two", "2" }
        };

        List<MqttUserProperty> userProperties = headers.ToUserProperties();

        userProperties.ShouldBe(
        [
            new MqttUserProperty("one", "1"),
            new MqttUserProperty("two", "2")
        ]);
    }
}
