// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Messaging.Configuration.Mqtt;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public class MqttClientWebSocketProxyConfigurationBuilderTests
{
    [Fact]
    public void WithAddress_ShouldSetAddress()
    {
        MqttClientWebSocketProxyConfigurationBuilder builder = new();

        builder.WithAddress("http://proxy:8080");

        MqttClientWebSocketProxyConfiguration configuration = builder.Build();
        configuration.Address.ShouldBe("http://proxy:8080");
    }

    [Fact]
    public void UseDefaultCredentials_ShouldEnableDefaultCredentialsAndClearUserPassword()
    {
        MqttClientWebSocketProxyConfigurationBuilder builder = new();

        builder.WithCredentials("user", "pass");
        builder.UseDefaultCredentials();

        MqttClientWebSocketProxyConfiguration configuration = builder.Build();
        configuration.UseDefaultCredentials.ShouldBeTrue();
        configuration.Username.ShouldBeNull();
        configuration.Password.ShouldBeNull();
    }

    [Fact]
    public void WithCredentials_ShouldSetUsernameAndPasswordAndDisableDefaultCredentials()
    {
        MqttClientWebSocketProxyConfigurationBuilder builder = new();

        builder.UseDefaultCredentials();
        builder.WithCredentials("user", "pass");

        MqttClientWebSocketProxyConfiguration configuration = builder.Build();
        configuration.UseDefaultCredentials.ShouldBeFalse();
        configuration.Username.ShouldBe("user");
        configuration.Password.ShouldBe("pass");
    }

    [Fact]
    public void WithDomain_ShouldSetDomain()
    {
        MqttClientWebSocketProxyConfigurationBuilder builder = new();

        builder.WithDomain("DOMAIN");

        MqttClientWebSocketProxyConfiguration configuration = builder.Build();
        configuration.Domain.ShouldBe("DOMAIN");
    }

    [Fact]
    public void EnableBypassOnLocal_ShouldSetBypassOnLocalTrue()
    {
        MqttClientWebSocketProxyConfigurationBuilder builder = new();

        builder.EnableBypassOnLocal();

        MqttClientWebSocketProxyConfiguration configuration = builder.Build();
        configuration.BypassOnLocal.ShouldBeTrue();
    }

    [Fact]
    public void DisableBypassOnLocal_ShouldSetBypassOnLocalFalse()
    {
        MqttClientWebSocketProxyConfigurationBuilder builder = new();

        builder.EnableBypassOnLocal();
        builder.DisableBypassOnLocal();

        MqttClientWebSocketProxyConfiguration configuration = builder.Build();
        configuration.BypassOnLocal.ShouldBeFalse();
    }

    [Fact]
    public void WithBypassList_ShouldSetBypassList()
    {
        MqttClientWebSocketProxyConfigurationBuilder builder = new();

        string[] list = ["*.local", "localhost"];
        builder.WithBypassList(list);

        MqttClientWebSocketProxyConfiguration configuration = builder.Build();
        configuration.BypassList.ShouldBe(list);
    }
}
