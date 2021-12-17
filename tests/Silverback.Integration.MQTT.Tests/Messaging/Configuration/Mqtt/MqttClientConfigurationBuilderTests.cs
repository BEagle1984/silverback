// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using MQTTnet.Client.ExtendedAuthenticationExchange;
using MQTTnet.Diagnostics.PacketInspection;
using MQTTnet.Formatter;
using NSubstitute;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public class MqttClientConfigurationBuilderTests
{
    [Fact]
    public void Default_ProtocolVersionV500Set()
    {
        MqttClientConfigurationBuilder builder = new();

        builder.ConnectViaTcp("tests-server");

        MqttClientConfiguration config = builder.Build();
        config.ProtocolVersion.Should().Be(MqttProtocolVersion.V500);
    }

    [Fact]
    public void UseProtocolVersion_ProtocolVersionSet()
    {
        MqttClientConfigurationBuilder builder = new();

        builder
            .ConnectViaTcp("tests-server")
            .UseProtocolVersion(MqttProtocolVersion.V311);

        MqttClientConfiguration config = builder.Build();
        config.ProtocolVersion.Should().Be(MqttProtocolVersion.V311);
    }

    [Fact]
    public void WithCommunicationTimeout_TimeSpan_TimeoutSet()
    {
        MqttClientConfigurationBuilder builder = new();

        builder
            .ConnectViaTcp("tests-server")
            .WithCommunicationTimeout(TimeSpan.FromSeconds(42));

        MqttClientConfiguration config = builder.Build();
        config.CommunicationTimeout.TotalSeconds.Should().Be(42);
    }

    [Fact]
    public void RequestCleanSession_CleanSessionSetToTrue()
    {
        MqttClientConfigurationBuilder builder = new();

        builder
            .ConnectViaTcp("tests-server")
            .RequestCleanSession();

        MqttClientConfiguration config = builder.Build();
        config.CleanSession.Should().BeTrue();
    }

    [Fact]
    public void RequestPersistentSession_CleanSessionSetToFalse()
    {
        MqttClientConfigurationBuilder builder = new();

        builder
            .ConnectViaTcp("tests-server")
            .RequestPersistentSession();

        MqttClientConfiguration config = builder.Build();
        config.CleanSession.Should().BeFalse();
    }

    [Fact]
    public void DisableKeepAlive_KeepAliveIntervalSetToZero()
    {
        MqttClientConfigurationBuilder builder = new();

        builder
            .ConnectViaTcp("tests-server")
            .DisableKeepAlive();

        MqttClientConfiguration config = builder.Build();
        config.KeepAlivePeriod.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void SendKeepAlive_TimeSpan_KeepAliveIntervalSet()
    {
        MqttClientConfigurationBuilder builder = new();

        builder
            .ConnectViaTcp("tests-server")
            .SendKeepAlive(TimeSpan.FromMinutes(42));

        MqttClientConfiguration config = builder.Build();
        config.KeepAlivePeriod.Should().Be(TimeSpan.FromMinutes(42));
    }

    [Fact]
    public void WithClientId_String_ClientIdSet()
    {
        MqttClientConfigurationBuilder builder = new();

        builder
            .ConnectViaTcp("tests-server")
            .WithClientId("client-42");

        MqttClientConfiguration config = builder.Build();
        config.ClientId.Should().Be("client-42");
    }

    [Fact]
    public void SendLastWillMessage_Action_WillMessageAndDelaySet()
    {
        MqttClientConfigurationBuilder builder = new();

        builder
            .ConnectViaTcp("tests-server")
            .SendLastWillMessage<TestEventOne>(
                lastWill => lastWill
                    .Message(
                        new TestEventOne
                        {
                            Content = "I died!"
                        })
                    .WithDelay(TimeSpan.FromSeconds(42))
                    .ProduceTo("testaments"));

        MqttClientConfiguration config = builder.Build();
        config.WillMessage.Should().NotBeNull();
        config.WillMessage!.Topic.Should().Be("testaments");
        config.WillMessage.Payload.Should().NotBeNullOrEmpty();
        config.WillDelayInterval.Should().Be(42);
    }

    [Fact]
    public void WithAuthentication_Method_AuthenticationMethodAndDataSet()
    {
        MqttClientConfigurationBuilder builder = new();

        builder
            .ConnectViaTcp("tests-server")
            .WithAuthentication("method", null);

        MqttClientConfiguration config = builder.Build();
        config.AuthenticationMethod.Should().Be("method");
        config.AuthenticationData.Should().BeNull();
    }

    [Fact]
    public void WithAuthentication_MethodAndData_AuthenticationMethodAndDataSet()
    {
        MqttClientConfigurationBuilder builder = new();

        builder
            .ConnectViaTcp("tests-server")
            .WithAuthentication("method", new byte[] { 0x01, 0x02, 0x03 });

        MqttClientConfiguration config = builder.Build();
        config.AuthenticationMethod.Should().Be("method");
        config.AuthenticationData.Should().BeEquivalentTo(new byte[] { 0x01, 0x02, 0x03 });
    }

    [Fact]
    public void LimitTopicAlias_Int_TopicAliasMaximum()
    {
        MqttClientConfigurationBuilder builder = new();

        builder
            .ConnectViaTcp("tests-server")
            .LimitTopicAlias(42);

        MqttClientConfiguration config = builder.Build();
        config.TopicAliasMaximum.Should().Be(42);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(42, true)]
    [InlineData(ushort.MaxValue, true)]
    [InlineData(ushort.MaxValue + 1, false)]
    [InlineData(int.MaxValue, false)]
    [InlineData(-1, false)]
    public void LimitTopicAlias_Int_RangeValidated(int value, bool isValid)
    {
        MqttClientConfigurationBuilder builder = new();

        Action act = () => builder.LimitTopicAlias(value);

        if (isValid)
            act.Should().NotThrow();
        else
            act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void LimitPacketSize_Long_MaximumPacketSizeSet()
    {
        MqttClientConfigurationBuilder builder = new();

        builder
            .ConnectViaTcp("tests-server")
            .LimitPacketSize(42);

        MqttClientConfiguration config = builder.Build();
        config.MaximumPacketSize.Should().Be(42);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(42, true)]
    [InlineData(uint.MaxValue, true)]
    [InlineData((long)uint.MaxValue + 1, false)]
    [InlineData(long.MaxValue, false)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void LimitPacketSize_Long_RangeValidated(long value, bool isValid)
    {
        MqttClientConfigurationBuilder builder = new();

        Action act = () => builder.LimitPacketSize(value);

        if (isValid)
            act.Should().NotThrow();
        else
            act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void LimitUnacknowledgedPublications_Int_ReceiveMaximumSet()
    {
        MqttClientConfigurationBuilder builder = new();

        builder
            .ConnectViaTcp("tests-server")
            .LimitUnacknowledgedPublications(42);

        MqttClientConfiguration config = builder.Build();
        config.ReceiveMaximum.Should().Be(42);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(42, true)]
    [InlineData(ushort.MaxValue, true)]
    [InlineData(ushort.MaxValue + 1, false)]
    [InlineData(int.MaxValue, false)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void LimitUnacknowledgedPublications_Int_RangeValidated(int value, bool isValid)
    {
        MqttClientConfigurationBuilder builder = new();

        Action act = () => builder.LimitUnacknowledgedPublications(value);

        if (isValid)
            act.Should().NotThrow();
        else
            act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RequestProblemInformation_RequestProblemInformationSetToTrue()
    {
        MqttClientConfigurationBuilder builder = new();

        builder
            .ConnectViaTcp("tests-server")
            .RequestProblemInformation();

        MqttClientConfiguration config = builder.Build();
        config.RequestProblemInformation.Should().BeTrue();
    }

    [Fact]
    public void DisableProblemInformation_RequestProblemInformationSetToFalse()
    {
        MqttClientConfigurationBuilder builder = new();

        builder
            .ConnectViaTcp("tests-server")
            .DisableProblemInformation();

        MqttClientConfiguration config = builder.Build();
        config.RequestProblemInformation.Should().BeFalse();
    }

    [Fact]
    public void RequestResponseInformation_RequestResponseInformationSetToTrue()
    {
        MqttClientConfigurationBuilder builder = new();

        builder
            .ConnectViaTcp("tests-server")
            .RequestResponseInformation();

        MqttClientConfiguration config = builder.Build();
        config.RequestResponseInformation.Should().BeTrue();
    }

    [Fact]
    public void DisableResponseInformation_RequestResponseInformationSetToFalse()
    {
        MqttClientConfigurationBuilder builder = new();

        builder
            .ConnectViaTcp("tests-server")
            .DisableResponseInformation();

        MqttClientConfiguration config = builder.Build();
        config.RequestResponseInformation.Should().BeFalse();
    }

    [Fact]
    public void WithSessionExpiration_TimeSpan_SessionExpiryIntervalSet()
    {
        MqttClientConfigurationBuilder builder = new();

        builder
            .ConnectViaTcp("tests-server")
            .WithSessionExpiration(TimeSpan.FromSeconds(42));

        MqttClientConfiguration config = builder.Build();
        config.SessionExpiryInterval.Should().Be(42);
    }

    [Fact]
    public void AddUserProperty_NamesAndValues_UserPropertiesAdded()
    {
        MqttClientConfigurationBuilder builder = new();

        builder
            .ConnectViaTcp("tests-server")
            .AddUserProperty("prop1", "value1")
            .AddUserProperty("prop2", "value2");

        MqttClientConfiguration config = builder.Build();
        config.UserProperties.Should().BeEquivalentTo(
            new[]
            {
                new MqttUserProperty("prop1", "value1"),
                new MqttUserProperty("prop2", "value2")
            });
    }

    [Fact]
    public void WithCredentials_UserNameAndPasswordString_CredentialsSet()
    {
        MqttClientConfigurationBuilder builder = new();

        builder
            .ConnectViaTcp("tests-server")
            .WithCredentials("user", "pass");

        MqttClientConfiguration config = builder.Build();
        config.Credentials.Should().NotBeNull();
        config.Credentials!.Username.Should().Be("user");
        config.Credentials!.Password.Should().BeEquivalentTo(Encoding.UTF8.GetBytes("pass"));
    }

    [Fact]
    public void WithCredentials_UserNameAndPasswordBytes_CredentialsSet()
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes("pass");
        MqttClientConfigurationBuilder builder = new();

        builder
            .ConnectViaTcp("tests-server")
            .WithCredentials("user", passwordBytes);

        MqttClientConfiguration config = builder.Build();
        config.Credentials.Should().NotBeNull();
        config.Credentials!.Username.Should().Be("user");
        config.Credentials!.Password.Should().BeEquivalentTo(passwordBytes);
    }

    [Fact]
    public void WithCredentials_ClientCredentials_CredentialsSet()
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes("pass");
        MqttClientConfigurationBuilder builder = new();

        builder
            .ConnectViaTcp("tests-server")
            .WithCredentials(new MqttClientCredentials("user", passwordBytes));

        MqttClientConfiguration config = builder.Build();
        config.Credentials.Should().NotBeNull();
        config.Credentials!.Username.Should().Be("user");
        config.Credentials!.Password.Should().BeEquivalentTo(passwordBytes);
    }

    [Fact]
    public void UseExtendedAuthenticationExchangeHandler_Instance_HandlerSet()
    {
        TestExtendedAuthenticationExchangeHandler instance = new();
        MqttClientConfigurationBuilder builder = new();

        builder
            .ConnectViaTcp("tests-server")
            .UseExtendedAuthenticationExchangeHandler(instance);

        MqttClientConfiguration config = builder.Build();
        config.ExtendedAuthenticationExchangeHandler.Should().BeSameAs(instance);
    }

    [Fact]
    public void UseExtendedAuthenticationExchangeHandler_GenericTypeArgument_HandlerSet()
    {
        IServiceProvider? serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(TestExtendedAuthenticationExchangeHandler))
            .Returns(new TestExtendedAuthenticationExchangeHandler());

        MqttClientConfigurationBuilder builder = new(serviceProvider);

        builder
            .ConnectViaTcp("tests-server")
            .UseExtendedAuthenticationExchangeHandler<TestExtendedAuthenticationExchangeHandler>();

        MqttClientConfiguration config = builder.Build();
        config.ExtendedAuthenticationExchangeHandler.Should()
            .BeOfType<TestExtendedAuthenticationExchangeHandler>();
    }

    [Fact]
    public void UseExtendedAuthenticationExchangeHandler_Type_HandlerSet()
    {
        IServiceProvider? serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(TestExtendedAuthenticationExchangeHandler))
            .Returns(new TestExtendedAuthenticationExchangeHandler());

        MqttClientConfigurationBuilder builder = new(serviceProvider);

        builder
            .ConnectViaTcp("tests-server")
            .UseExtendedAuthenticationExchangeHandler(typeof(TestExtendedAuthenticationExchangeHandler));

        MqttClientConfiguration config = builder.Build();
        config.ExtendedAuthenticationExchangeHandler.Should()
            .BeOfType<TestExtendedAuthenticationExchangeHandler>();
    }

    [Fact]
    public void ConnectViaTcp_ServerAndPort_ChannelSet()
    {
        MqttClientConfigurationBuilder builder = new();

        builder.ConnectViaTcp("tests-server", 1234);

        MqttClientConfiguration config = builder.Build();
        config.Channel.Should().BeOfType<MqttClientTcpConfiguration>();
        config.Channel.As<MqttClientTcpConfiguration>().Server.Should().Be("tests-server");
        config.Channel.As<MqttClientTcpConfiguration>().Port.Should().Be(1234);
    }

    [Fact]
    public void ConnectViaTcp_Options_ChannelSet()
    {
        MqttClientConfigurationBuilder builder = new();

        builder.ConnectViaTcp(
            new MqttClientTcpConfiguration
            {
                Server = "tests-server",
                Port = 1234
            });

        MqttClientConfiguration config = builder.Build();
        config.Channel.Should().BeOfType<MqttClientTcpConfiguration>();
        config.Channel.As<MqttClientTcpConfiguration>().Server.Should().Be("tests-server");
        config.Channel.As<MqttClientTcpConfiguration>().Port.Should().Be(1234);
    }

    [Fact]
    public void ConnectViaWebSocket_Uri_ChannelSet()
    {
        MqttClientConfigurationBuilder builder = new();

        builder.ConnectViaWebSocket("uri");

        MqttClientConfiguration config = builder.Build();
        config.Channel.Should().BeOfType<MqttClientWebSocketConfiguration>();
        config.Channel.As<MqttClientWebSocketConfiguration>().Uri.Should().Be("uri");
    }

    [Fact]
    public void ConnectViaWebSocket_Options_ChannelSet()
    {
        MqttClientConfigurationBuilder builder = new();

        builder.ConnectViaWebSocket(
            new MqttClientWebSocketConfiguration
            {
                Uri = "uri",
                RequestHeaders = new Dictionary<string, string>
                {
                    { "header", "value" }
                }
            });

        MqttClientConfiguration config = builder.Build();
        config.Channel.Should().BeOfType<MqttClientWebSocketConfiguration>();
        config.Channel.As<MqttClientWebSocketConfiguration>().Uri.Should().Be("uri");
        config.Channel.As<MqttClientWebSocketConfiguration>().RequestHeaders.Should().BeEquivalentTo(
            new Dictionary<string, string>
            {
                { "header", "value" }
            });
    }

    [Fact]
    public void UseProxy_Data_ProxySet()
    {
        MqttClientConfigurationBuilder builder = new();

        builder
            .ConnectViaWebSocket("uri")
            .UseProxy(
                "address",
                "user",
                "pass",
                "domain",
                true,
                new[] { "local1", "local2" });

        MqttClientConfiguration config = builder.Build();
        config.Channel.As<MqttClientWebSocketConfiguration>().Proxy.Should().NotBeNull();
        config.Channel.As<MqttClientWebSocketConfiguration>().Proxy!.Address.Should().Be("address");
        config.Channel.As<MqttClientWebSocketConfiguration>().Proxy!.Username.Should().Be("user");
        config.Channel.As<MqttClientWebSocketConfiguration>().Proxy!.Password.Should().Be("pass");
        config.Channel.As<MqttClientWebSocketConfiguration>().Proxy!.Domain.Should().Be("domain");
        config.Channel.As<MqttClientWebSocketConfiguration>().Proxy!.BypassOnLocal.Should().BeTrue();
        config.Channel.As<MqttClientWebSocketConfiguration>().Proxy!.BypassList.Should().BeEquivalentTo("local1", "local2");
    }

    [Fact]
    public void UseProxy_Options_ProxySet()
    {
        MqttClientConfigurationBuilder builder = new();

        builder
            .ConnectViaWebSocket("uri")
            .UseProxy(
                new MqttClientWebSocketProxyConfiguration
                {
                    Address = "address",
                    Username = "user",
                    Password = "pass",
                    Domain = "domain",
                    BypassOnLocal = true,
                    BypassList = new[] { "local1", "local2" }
                });

        MqttClientConfiguration config = builder.Build();
        config.Channel.As<MqttClientWebSocketConfiguration>().Proxy.Should().NotBeNull();
        config.Channel.As<MqttClientWebSocketConfiguration>().Proxy!.Address.Should().Be("address");
        config.Channel.As<MqttClientWebSocketConfiguration>().Proxy!.Username.Should().Be("user");
        config.Channel.As<MqttClientWebSocketConfiguration>().Proxy!.Password.Should().Be("pass");
        config.Channel.As<MqttClientWebSocketConfiguration>().Proxy!.Domain.Should().Be("domain");
        config.Channel.As<MqttClientWebSocketConfiguration>().Proxy!.BypassOnLocal.Should().BeTrue();
        config.Channel.As<MqttClientWebSocketConfiguration>().Proxy!.BypassList.Should().BeEquivalentTo("local1", "local2");
    }

    [Fact]
    public void EnableTls_Tcp_UseTlsSetToTrue()
    {
        MqttClientConfigurationBuilder builder = new();

        builder
            .ConnectViaTcp("tests-server")
            .EnableTls();

        MqttClientConfiguration config = builder.Build();
        config.Channel.As<MqttClientTcpConfiguration>().Tls.UseTls.Should().BeTrue();
    }

    [Fact]
    public void DisableTls_Tcp_UseTlsSetToFalse()
    {
        MqttClientConfigurationBuilder builder = new();

        builder
            .ConnectViaTcp("tests-server")
            .DisableTls();

        MqttClientConfiguration config = builder.Build();
        config.Channel.As<MqttClientTcpConfiguration>().Tls.UseTls.Should().BeFalse();
    }

    [Fact]
    public void EnableTls_WebSocket_UseTlsSetToTrue()
    {
        MqttClientConfigurationBuilder builder = new();

        builder
            .ConnectViaWebSocket("tests-server")
            .EnableTls();

        MqttClientConfiguration config = builder.Build();
        config.Channel.As<MqttClientWebSocketConfiguration>().Tls.UseTls.Should().BeTrue();
    }

    [Fact]
    public void DisableTls_WebSocket_UseTlsSetToFalse()
    {
        MqttClientConfigurationBuilder builder = new();

        builder
            .ConnectViaWebSocket("tests-server")
            .DisableTls();

        MqttClientConfiguration config = builder.Build();
        config.Channel.As<MqttClientWebSocketConfiguration>().Tls.UseTls.Should().BeFalse();
    }

    [Fact]
    public void EnableTls_Options_TlsParametersSet()
    {
        MqttClientConfigurationBuilder builder = new();

        builder
            .ConnectViaTcp("tests-server")
            .EnableTls(
                new MqttClientTlsConfiguration
                {
                    UseTls = true,
                    SslProtocol = SslProtocols.Tls12,
                    AllowUntrustedCertificates = true
                });

        MqttClientConfiguration config = builder.Build();
        config.Channel.As<MqttClientTcpConfiguration>().Tls.UseTls.Should().BeTrue();
        config.Channel.As<MqttClientTcpConfiguration>().Tls.SslProtocol.Should().Be(SslProtocols.Tls12);
        config.Channel.As<MqttClientTcpConfiguration>().Tls.AllowUntrustedCertificates.Should().BeTrue();
    }

    [Fact]
    public void UsePacketInspector_Instance_InspectorSet()
    {
        TestPacketInspector instance = new();
        MqttClientConfigurationBuilder builder = new();

        builder
            .ConnectViaTcp("tests-server")
            .UsePacketInspector(instance);

        MqttClientConfiguration config = builder.Build();
        config.PacketInspector.Should().BeSameAs(instance);
    }

    [Fact]
    public void UsePacketInspector_GenericTypeArgument_InspectorSet()
    {
        IServiceProvider? serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(TestPacketInspector)).Returns(new TestPacketInspector());

        MqttClientConfigurationBuilder builder = new(serviceProvider);

        builder
            .ConnectViaTcp("tests-server")
            .UsePacketInspector<TestPacketInspector>();

        MqttClientConfiguration config = builder.Build();
        config.PacketInspector.Should().BeOfType<TestPacketInspector>();
    }

    [Fact]
    public void UsePacketInspector_Type_InspectorSet()
    {
        IServiceProvider? serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(TestPacketInspector)).Returns(new TestPacketInspector());

        MqttClientConfigurationBuilder builder = new(serviceProvider);

        builder
            .ConnectViaTcp("tests-server")
            .UsePacketInspector(typeof(TestPacketInspector));

        MqttClientConfiguration config = builder.Build();
        config.PacketInspector.Should().BeOfType<TestPacketInspector>();
    }

    [Fact]
    public void Constructor_WithTcpBaseConfig_InitializedFromBaseConfig()
    {
        MqttClientConfigurationBuilder builder = new();

        builder
            .UseProtocolVersion(MqttProtocolVersion.V311)
            .ConnectViaTcp("tests-server", 1234)
            .EnableTls()
            .UseExtendedAuthenticationExchangeHandler(new TestExtendedAuthenticationExchangeHandler());

        MqttClientConfiguration baseConfig = builder.Build();
        MqttClientConfiguration config = new MqttClientConfigurationBuilder(baseConfig).Build();

        config.Should().BeEquivalentTo(baseConfig);
        config.Should().NotBeSameAs(baseConfig);
    }

    [Fact]
    public void Constructor_WithWebSocketBaseConfig_InitializedFromBaseConfig()
    {
        MqttClientConfigurationBuilder builder = new();

        builder
            .UseProtocolVersion(MqttProtocolVersion.V311)
            .ConnectViaWebSocket("uri")
            .UseProxy(
                new MqttClientWebSocketProxyConfiguration
                {
                    Address = "address",
                    Username = "user",
                    Password = "pass",
                    Domain = "domain",
                    BypassOnLocal = true,
                    BypassList = new[] { "local1", "local2" }
                })
            .EnableTls(
                new MqttClientTlsConfiguration
                {
                    UseTls = true,
                    SslProtocol = SslProtocols.Tls12,
                    AllowUntrustedCertificates = true
                })
            .UseExtendedAuthenticationExchangeHandler(new TestExtendedAuthenticationExchangeHandler());

        MqttClientConfiguration baseConfig = builder.Build();
        MqttClientConfiguration config = new MqttClientConfigurationBuilder(baseConfig).Build();

        config.Should().BeEquivalentTo(baseConfig);
        config.Should().NotBeSameAs(baseConfig);
    }

    private sealed class TestExtendedAuthenticationExchangeHandler : IMqttExtendedAuthenticationExchangeHandler
    {
        public Task HandleRequestAsync(MqttExtendedAuthenticationExchangeContext context) => Task.CompletedTask;
    }

    private sealed class TestPacketInspector : IMqttPacketInspector
    {
        public void ProcessMqttPacket(ProcessMqttPacketContext context)
        {
        }
    }
}
