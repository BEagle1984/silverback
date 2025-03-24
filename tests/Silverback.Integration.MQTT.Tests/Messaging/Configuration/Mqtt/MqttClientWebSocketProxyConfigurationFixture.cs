// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Shouldly;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Mqtt;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public class MqttClientWebSocketProxyConfigurationFixture
{
    [Fact]
    public void Validate_ShouldNotThrow_WhenIsValid()
    {
        MqttClientWebSocketProxyConfiguration configuration = GetValidConfiguration();

        Action act = configuration.Validate;

        act.ShouldNotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldThrow_WhenAddressIsNullOrWhitespace(string? address)
    {
        MqttClientWebSocketProxyConfiguration configuration = GetValidConfiguration() with
        {
            Address = address
        };

        Action act = configuration.Validate;

        act.ShouldThrow<BrokerConfigurationException>();
    }

    private static MqttClientWebSocketProxyConfiguration GetValidConfiguration() => new()
    {
        Address = "ws://test:123"
    };
}
