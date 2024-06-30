// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
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

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_ShouldThrow_WhenNameIsNullOrEmpty(string? name)
    {
        MqttUserProperty configuration = new(name!, "value");

        Action act = configuration.Validate;

        act.Should().Throw<BrokerConfigurationException>().WithMessage("The name of a user property cannot be empty.");
    }
}
