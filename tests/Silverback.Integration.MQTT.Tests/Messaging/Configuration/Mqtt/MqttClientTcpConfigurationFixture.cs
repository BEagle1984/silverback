// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Net;
using Shouldly;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Mqtt;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public class MqttClientTcpConfigurationFixture
{
    [Fact]
    public void Validate_ShouldNotThrow_WhenIsValid()
    {
        MqttClientTcpConfiguration configuration = GetValidConfiguration();

        Action act = configuration.Validate;

        act.ShouldNotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenRemoteEndpointIsNull()
    {
        MqttClientTcpConfiguration configuration = GetValidConfiguration() with { RemoteEndpoint = null };

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldBe("The remote endpoint is required to connect with the message broker.");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenTlsIsNull()
    {
        MqttClientTcpConfiguration configuration = GetValidConfiguration() with { Tls = null! };

        Action act = configuration.Validate;

        Exception exception = act.ShouldThrow<BrokerConfigurationException>();
        exception.Message.ShouldBe("The TLS configuration is required.");
    }

    private static MqttClientTcpConfiguration GetValidConfiguration() => new()
    {
        RemoteEndpoint = new DnsEndPoint("test", 1883),
        Tls = new MqttClientTlsConfiguration
        {
            UseTls = false
        }
    };
}
