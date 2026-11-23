// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using MQTTnet;
using Shouldly;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Mqtt;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public class MqttClientWebSocketConfigurationTests
{
    [Fact]
    public void Validate_ShouldNotThrow_WhenIsValid()
    {
        MqttClientWebSocketConfiguration configuration = GetValidConfiguration();

        Action act = configuration.Validate;

        act.ShouldNotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_ShouldThrow_WhenUriIsNullOrEmpty(string? uri)
    {
        MqttClientWebSocketConfiguration configuration = GetValidConfiguration() with { Uri = uri };

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldBe("Channel.Uri is required.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenTlsIsNull()
    {
        MqttClientWebSocketConfiguration configuration = GetValidConfiguration() with { Tls = null! };

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldBe("Channel.Tls is required.");
    }

    [Fact]
    public void ToMqttNetType_ShouldMapProxy()
    {
        MqttClientWebSocketConfiguration configuration = GetValidConfiguration() with
        {
            Proxy = new MqttClientWebSocketProxyConfiguration
            {
                Address = "http://proxy:8080",
                Username = "user",
                Password = "pass",
                BypassOnLocal = true
            }
        };

        IMqttClientChannelOptions options = configuration.ToMqttNetType();

        MqttClientWebSocketProxyOptions? proxyOptions = options.ShouldBeOfType<MqttClientWebSocketOptions>().ProxyOptions;
        proxyOptions.ShouldNotBeNull();
        proxyOptions.Address.ShouldBe("http://proxy:8080");
        proxyOptions.Username.ShouldBe("user");
        proxyOptions.Password.ShouldBe("pass");
        proxyOptions.BypassOnLocal.ShouldBeTrue();
    }

    [Fact]
    public void ToMqttNetType_ShouldNotSetProxy_WhenProxyIsNull()
    {
        MqttClientWebSocketConfiguration configuration = GetValidConfiguration();

        IMqttClientChannelOptions options = configuration.ToMqttNetType();

        options.ShouldBeOfType<MqttClientWebSocketOptions>().ProxyOptions.ShouldBeNull();
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
