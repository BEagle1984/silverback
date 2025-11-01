// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Messaging.Configuration.Mqtt;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public class MqttClientWebSocketConfigurationBuilderTests
{
    [Fact]
    public void WithUri_ShouldSetUri()
    {
        MqttClientWebSocketConfigurationBuilder builder = new();

        builder.WithUri("ws://test:1883/mqtt");

        MqttClientWebSocketConfiguration configuration = builder.Build();
        configuration.Uri.ShouldBe("ws://test:1883/mqtt");
    }

    [Fact]
    public void UseProxy_ShouldSetProxyAddress()
    {
        MqttClientWebSocketConfigurationBuilder builder = new();

        builder.UseProxy("http://proxy:8080");

        MqttClientWebSocketConfiguration configuration = builder.Build();
        configuration.Proxy.ShouldNotBeNull();
        configuration.Proxy.Address.ShouldBe("http://proxy:8080");
    }

    [Fact]
    public void UseProxy_ShouldSetProxyConfiguration()
    {
        MqttClientWebSocketConfigurationBuilder builder = new();

        builder.UseProxy("http://proxy:8080", proxy => proxy
            .WithCredentials("user", "pass")
            .WithDomain("domain")
            .EnableBypassOnLocal()
            .WithBypassList(["*.local", "localhost"]));

        MqttClientWebSocketConfiguration configuration = builder.Build();
        configuration.Proxy.ShouldNotBeNull();
        configuration.Proxy.Address.ShouldBe("http://proxy:8080");
        configuration.Proxy.UseDefaultCredentials.ShouldBeFalse();
        configuration.Proxy.Username.ShouldBe("user");
        configuration.Proxy.Password.ShouldBe("pass");
        configuration.Proxy.Domain.ShouldBe("domain");
        configuration.Proxy.BypassOnLocal.ShouldBeTrue();
        configuration.Proxy.BypassList.ShouldBe(["*.local", "localhost"]);
    }
}
