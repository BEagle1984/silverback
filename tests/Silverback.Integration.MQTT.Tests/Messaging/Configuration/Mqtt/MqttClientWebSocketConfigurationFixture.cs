// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Mqtt;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public class MqttClientWebSocketConfigurationFixture
{
    [Fact]
    public void Validate_ShouldNotThrow_WhenIsValid()
    {
        MqttClientWebSocketConfiguration configuration = GetValidConfiguration();

        Action act = configuration.Validate;

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_ShouldThrow_WhenUriIsNullOrEmpty(string? uri)
    {
        MqttClientWebSocketConfiguration configuration = GetValidConfiguration() with { Uri = uri };

        Action act = configuration.Validate;

        act.Should().Throw<BrokerConfigurationException>().WithMessage("The URI is required to connect with the message broker.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenTlsIsNull()
    {
        MqttClientWebSocketConfiguration configuration = GetValidConfiguration() with { Tls = null! };

        Action act = configuration.Validate;

        act.Should().Throw<BrokerConfigurationException>().WithMessage("The TLS configuration is required.");
    }

    private static MqttClientWebSocketConfiguration GetValidConfiguration() => new()
    {
        Uri = "ws://test:1883/mqtt",
        Tls = new MqttClientTlsConfiguration
        {
            UseTls = false
        }
    };
}
