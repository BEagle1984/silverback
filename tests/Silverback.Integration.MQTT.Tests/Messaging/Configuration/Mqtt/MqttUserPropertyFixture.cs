// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Shouldly;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Mqtt;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public class MqttUserPropertyFixture
{
    [Fact]
    public void Validate_ShouldNotThrow_WhenIsValid()
    {
        MqttUserProperty configuration = new("name", "value");

        Action act = configuration.Validate;

        act.ShouldNotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_ShouldThrow_WhenNameIsNullOrEmpty(string? name)
    {
        MqttUserProperty configuration = new(name!, "value");

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldBe("The name of a user property cannot be empty.");
    }
}
