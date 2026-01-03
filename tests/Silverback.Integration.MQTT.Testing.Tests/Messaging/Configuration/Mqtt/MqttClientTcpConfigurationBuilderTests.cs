// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Net;
using System.Net.Sockets;
using Shouldly;
using Silverback.Messaging.Configuration.Mqtt;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Testing.Messaging.Configuration.Mqtt;

public class MqttClientTcpConfigurationBuilderTests
{
    [Fact]
    public void WithAddressFamily_ShouldSetAddressFamily()
    {
        MqttClientTcpConfigurationBuilder builder = new();

        builder.WithAddressFamily(AddressFamily.InterNetworkV6);

        MqttClientTcpConfiguration configuration = builder.Build();
        configuration.AddressFamily.ShouldBe(AddressFamily.InterNetworkV6);
    }

    [Fact]
    public void WithBufferSize_ShouldSetBufferSize()
    {
        MqttClientTcpConfigurationBuilder builder = new();

        builder.WithBufferSize(8192);

        MqttClientTcpConfiguration configuration = builder.Build();
        configuration.BufferSize.ShouldBe(8192);
    }

    [Fact]
    public void EnableDualMode_ShouldEnableDualMode()
    {
        MqttClientTcpConfigurationBuilder builder = new();

        builder.EnableDualMode();

        MqttClientTcpConfiguration configuration = builder.Build();
        configuration.DualMode.ShouldBe(true);
    }

    [Fact]
    public void DisableDualMode_ShouldDisableDualMode()
    {
        MqttClientTcpConfigurationBuilder builder = new();

        builder.DisableDualMode();

        MqttClientTcpConfiguration configuration = builder.Build();
        configuration.DualMode.ShouldBe(false);
    }

    [Fact]
    public void EnableLinger_ShouldSetLingerState()
    {
        MqttClientTcpConfigurationBuilder builder = new();

        builder.EnableLinger(10);

        MqttClientTcpConfiguration configuration = builder.Build();
        configuration.LingerState.ShouldNotBeNull();
        configuration.LingerState.Enabled.ShouldBeTrue();
        configuration.LingerState.LingerTime.ShouldBe(10);
    }

    [Fact]
    public void DisableLinger_ShouldDisableLingerState()
    {
        MqttClientTcpConfigurationBuilder builder = new();

        builder.DisableLinger();

        MqttClientTcpConfiguration configuration = builder.Build();
        configuration.LingerState.ShouldNotBeNull();
        configuration.LingerState.Enabled.ShouldBeFalse();
    }

    [Fact]
    public void WithLocalEndpoint_ShouldSetLocalEndpoint()
    {
        MqttClientTcpConfigurationBuilder builder = new();

        IPEndPoint local = new(IPAddress.Loopback, 12345);
        builder.WithLocalEndpoint(local);

        MqttClientTcpConfiguration configuration = builder.Build();
        configuration.LocalEndpoint.ShouldBe(local);
    }

    [Fact]
    public void EnableNoDelay_ShouldSetNoDelayTrue()
    {
        MqttClientTcpConfigurationBuilder builder = new();

        builder.EnableNoDelay();

        MqttClientTcpConfiguration configuration = builder.Build();
        configuration.NoDelay.ShouldBeTrue();
    }

    [Fact]
    public void DisableNoDelay_ShouldSetNoDelayFalse()
    {
        MqttClientTcpConfigurationBuilder builder = new();

        builder.DisableNoDelay();

        MqttClientTcpConfiguration configuration = builder.Build();
        configuration.NoDelay.ShouldBeFalse();
    }

    [Fact]
    public void WithProtocolType_ShouldSetProtocolType()
    {
        MqttClientTcpConfigurationBuilder builder = new();

        builder.WithProtocolType(ProtocolType.Icmp);

        MqttClientTcpConfiguration configuration = builder.Build();
        configuration.ProtocolType.ShouldBe(ProtocolType.Icmp);
    }

    [Fact]
    public void WithRemoteEndpoint_EndPoint_ShouldSetRemoteEndpoint()
    {
        MqttClientTcpConfigurationBuilder builder = new();

        IPEndPoint remote = new(IPAddress.Parse("1.2.3.4"), 4321);
        builder.WithRemoteEndpoint(remote);

        MqttClientTcpConfiguration configuration = builder.Build();
        configuration.RemoteEndpoint.ShouldBe(remote);
    }

    [Fact]
    public void WithRemoteEndpoint_HostPort_ShouldSetDnsEndPoint()
    {
        MqttClientTcpConfigurationBuilder builder = new();

        builder.WithRemoteEndpoint("test-host", 1883);

        MqttClientTcpConfiguration configuration = builder.Build();
        configuration.RemoteEndpoint.ShouldBe(new DnsEndPoint("test-host", 1883));
    }

    [Fact]
    public void WithRemoteEndpoint_HostPortAndAddressFamily_ShouldSetDnsEndPointWithFamily()
    {
        MqttClientTcpConfigurationBuilder builder = new();

        builder.WithRemoteEndpoint("test-host", 8883, AddressFamily.InterNetworkV6);

        MqttClientTcpConfiguration configuration = builder.Build();
        configuration.RemoteEndpoint.ShouldBe(new DnsEndPoint("test-host", 8883, AddressFamily.InterNetworkV6));
    }
}
