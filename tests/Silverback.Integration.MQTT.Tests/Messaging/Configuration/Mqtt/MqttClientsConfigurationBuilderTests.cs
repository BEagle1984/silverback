// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Formatter;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Tests.Logging;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public class MqttClientsConfigurationBuilderTests
{
    [Fact]
    public void UseProtocolVersion_ShouldSetProtocolVersion()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.UseProtocolVersion(MqttProtocolVersion.V311);

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        configuration.ProtocolVersion.ShouldBe(MqttProtocolVersion.V311);
    }

    [Fact]
    public void WithTimeout_ShouldSetTimeout()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.WithTimeout(TimeSpan.FromSeconds(42));

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        configuration.Timeout.TotalSeconds.ShouldBe(42);
    }

    [Fact]
    public void RequestCleanSession_ShouldSetCleanSession()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.RequestCleanSession();

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        configuration.CleanSession.ShouldBeTrue();
    }

    [Fact]
    public void RequestPersistentSession_ShouldSetCleanSession()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.RequestPersistentSession();

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        configuration.CleanSession.ShouldBeFalse();
    }

    [Fact]
    public void DisableKeepAlive_ShouldSetKeepAliveInterval()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.DisableKeepAlive();

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        configuration.KeepAlivePeriod.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void SendKeepAlive_ShouldSetKeepAliveInterval()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.SendKeepAlive(TimeSpan.FromMinutes(42));

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        configuration.KeepAlivePeriod.ShouldBe(TimeSpan.FromMinutes(42));
    }

    [Fact]
    public void SendLastWillMessage_ShouldSetWillMessageAndDelay()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.SendLastWillMessage<TestEventOne>(lastWill => lastWill
            .SendMessage(
                new TestEventOne
                {
                    Content = "I died!"
                })
            .WithDelay(TimeSpan.FromSeconds(42))
            .ProduceTo("testaments"));

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        configuration.WillMessage.ShouldNotBeNull();
        configuration.WillMessage.Topic.ShouldBe("testaments");
        configuration.WillMessage.Payload.ShouldNotBeEmpty();
        configuration.WillMessage.Delay.ShouldBe(42U);
    }

    [Fact]
    public void WithEnhancedAuthentication_ShouldSetAuthenticationMethodAndData()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.WithEnhancedAuthentication("method", [0x01, 0x02, 0x03]);

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        configuration.AuthenticationMethod.ShouldBe("method");
        configuration.AuthenticationData.ShouldBe([0x01, 0x02, 0x03]);
    }

    [Fact]
    public void LimitTopicAlias_ShouldSetTopicAliasMaximum()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.LimitTopicAlias(42);

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        configuration.TopicAliasMaximum.ShouldBe((ushort)42);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(42, true)]
    [InlineData(ushort.MaxValue, true)]
    [InlineData(ushort.MaxValue + 1, false)]
    [InlineData(int.MaxValue, false)]
    [InlineData(-1, false)]
    public void LimitTopicAlias_ShouldValidateRange(int value, bool isValid)
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        Action act = () => builder.LimitTopicAlias(value);

        if (isValid)
            act.ShouldNotThrow();
        else
            act.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void LimitPacketSize_ShouldSetMaximumPacketSize()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.LimitPacketSize(42);

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        configuration.MaximumPacketSize.ShouldBe(42U);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(42, true)]
    [InlineData(uint.MaxValue, true)]
    [InlineData((long)uint.MaxValue + 1, false)]
    [InlineData(long.MaxValue, false)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void LimitPacketSize_ShouldValidateRange(long value, bool isValid)
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        Action act = () => builder.LimitPacketSize(value);

        if (isValid)
            act.ShouldNotThrow();
        else
            act.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void LimitUnacknowledgedPublications_ShouldSetReceiveMaximum()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.LimitUnacknowledgedPublications(42);

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        configuration.ReceiveMaximum.ShouldBe((ushort)42);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(42, true)]
    [InlineData(ushort.MaxValue, true)]
    [InlineData(ushort.MaxValue + 1, false)]
    [InlineData(int.MaxValue, false)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void LimitUnacknowledgedPublications_ShouldValidateRange(int value, bool isValid)
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        Action act = () => builder.LimitUnacknowledgedPublications(value);

        if (isValid)
            act.ShouldNotThrow();
        else
            act.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RequestProblemInformation_ShouldSetRequestProblemInformation()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.RequestProblemInformation();

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        configuration.RequestProblemInformation.ShouldBeTrue();
    }

    [Fact]
    public void DisableProblemInformation_ShouldSetRequestProblemInformation()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.DisableProblemInformation();

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        configuration.RequestProblemInformation.ShouldBeFalse();
    }

    [Fact]
    public void RequestResponseInformation_ShouldSetRequestResponseInformation()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.RequestResponseInformation();

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        configuration.RequestResponseInformation.ShouldBeTrue();
    }

    [Fact]
    public void DisableResponseInformation_ShouldSetRequestResponseInformation()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.DisableResponseInformation();

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        configuration.RequestResponseInformation.ShouldBeFalse();
    }

    [Fact]
    public void WithSessionExpiration_ShouldSetSessionExpiryInterval()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.WithSessionExpiration(TimeSpan.FromSeconds(42));

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        configuration.SessionExpiryInterval.ShouldBe(42U);
    }

    [Fact]
    public void AddUserProperty_ShouldAddUserProperties()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder
            .AddUserProperty("prop1", "value1")
            .AddUserProperty("prop2", "value2");

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        configuration.UserProperties.ShouldBe(
            [
                new MqttUserProperty("prop1", "value1"),
                new MqttUserProperty("prop2", "value2")
            ],
            true);
    }

    [Fact]
    public void WithCredentials_ShouldSetCredentialsWithStringPassword()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.WithCredentials("user", "pass");

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        configuration.Credentials.ShouldNotBeNull();
        configuration.Credentials.GetUserName(configuration.GetMqttClientOptions()).ShouldBe("user");
        configuration.Credentials.GetPassword(configuration.GetMqttClientOptions()).ShouldBe(Encoding.UTF8.GetBytes("pass"));
    }

    [Fact]
    public void WithCredentials_ShouldSetCredentialsWithBytesPassword()
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes("pass");
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.WithCredentials("user", passwordBytes);

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        configuration.Credentials.ShouldNotBeNull();
        configuration.Credentials.GetUserName(configuration.GetMqttClientOptions()).ShouldBe("user");
        configuration.Credentials.GetPassword(configuration.GetMqttClientOptions()).ShouldBe(passwordBytes);
    }

    [Fact]
    public void WithCredentials_ShouldSetCredentialsFromClientCredentialsProvider()
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes("pass");
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.WithCredentials(new MqttClientCredentials("user", passwordBytes));

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        configuration.Credentials.ShouldNotBeNull();
        configuration.Credentials.GetUserName(configuration.GetMqttClientOptions()).ShouldBe("user");
        configuration.Credentials.GetPassword(configuration.GetMqttClientOptions()).ShouldBe(passwordBytes);
    }

    [Fact]
    public void UseEnhancedAuthenticationHandler_ShouldSetHandlerFromInstance()
    {
        TestEnhancedAuthenticationHandler instance = new();
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.UseEnhancedAuthenticationHandler(instance);

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        configuration.EnhancedAuthenticationHandler.ShouldBeSameAs(instance);
    }

    [Fact]
    public void UseEnhancedAuthenticationHandler_ShouldSetHandlerFromGenericTypeArgument()
    {
        IServiceProvider? serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(TestEnhancedAuthenticationHandler))
            .Returns(new TestEnhancedAuthenticationHandler());

        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder
            .ConnectViaTcp("tests-server")
            .UseEnhancedAuthenticationHandler<TestEnhancedAuthenticationHandler>();

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint(serviceProvider);
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        configuration.EnhancedAuthenticationHandler.ShouldBeOfType<TestEnhancedAuthenticationHandler>();
    }

    [Fact]
    public void UseEnhancedAuthenticationHandler_ShouldSetHandlerFromType()
    {
        IServiceProvider? serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(TestEnhancedAuthenticationHandler))
            .Returns(new TestEnhancedAuthenticationHandler());

        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.UseEnhancedAuthenticationHandler<TestEnhancedAuthenticationHandler>();

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint(serviceProvider);
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        configuration.EnhancedAuthenticationHandler.ShouldBeOfType<TestEnhancedAuthenticationHandler>();
    }

    [Fact]
    public void ConnectTo_ShouldConnectViaTcp()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.ConnectTo("mqtt://test:42");

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        MqttClientTcpConfiguration tcpConfiguration = configuration.Channel.ShouldBeOfType<MqttClientTcpConfiguration>();
        tcpConfiguration.RemoteEndpoint.ShouldBe(new DnsEndPoint("test", 42));
    }

    [Fact]
    public void ConnectViaTcp_ShouldSetChannelFromServer()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.ConnectViaTcp("tests-server");

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        MqttClientTcpConfiguration tcpConfiguration = configuration.Channel.ShouldBeOfType<MqttClientTcpConfiguration>();
        tcpConfiguration.RemoteEndpoint.ShouldBe(new DnsEndPoint("tests-server", 1883));
    }

    [Fact]
    public void ConnectViaTcp_ShouldSetChannelFromServerAndBuilderAction()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.ConnectViaTcp("tests-server", tcp => tcp.WithProtocolType(ProtocolType.IcmpV6));

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        MqttClientTcpConfiguration tcpConfiguration = configuration.Channel.ShouldBeOfType<MqttClientTcpConfiguration>();
        tcpConfiguration.RemoteEndpoint.ShouldBe(new DnsEndPoint("tests-server", 1883));
        tcpConfiguration.ProtocolType.ShouldBe(ProtocolType.IcmpV6);
    }

    [Fact]
    public void ConnectViaTcp_ShouldSetChannelFromServerAndPort()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.ConnectViaTcp("tests-server", 1234);

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        MqttClientTcpConfiguration tcpConfiguration = configuration.Channel.ShouldBeOfType<MqttClientTcpConfiguration>();
        tcpConfiguration.RemoteEndpoint.ShouldBe(new DnsEndPoint("tests-server", 1234));
    }

    [Fact]
    public void ConnectViaTcp_ShouldSetChannelFromServerAndPortAndBuilderAction()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.ConnectViaTcp(
            "tests-server",
            1234,
            tcp => tcp
                .WithProtocolType(ProtocolType.IcmpV6)
                .WithAddressFamily(AddressFamily.InterNetworkV6));

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        MqttClientTcpConfiguration tcpConfiguration = configuration.Channel.ShouldBeOfType<MqttClientTcpConfiguration>();
        tcpConfiguration.RemoteEndpoint.ShouldBe(new DnsEndPoint("tests-server", 1234));
        tcpConfiguration.ProtocolType.ShouldBe(ProtocolType.IcmpV6);
        tcpConfiguration.AddressFamily.ShouldBe(AddressFamily.InterNetworkV6);
    }

    [Fact]
    public void ConnectViaTcp_ShouldSetChannelFromEndPoint()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.ConnectViaTcp(new IPEndPoint(IPAddress.Parse("1.2.3.4"), 1234));

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        MqttClientTcpConfiguration tcpConfiguration = configuration.Channel.ShouldBeOfType<MqttClientTcpConfiguration>();
        tcpConfiguration.RemoteEndpoint.ShouldBe(new IPEndPoint(IPAddress.Parse("1.2.3.4"), 1234));
    }

    [Fact]
    public void ConnectViaTcp_ShouldSetChannelFromEndPointAndBuilderAction()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.ConnectViaTcp(
            new IPEndPoint(IPAddress.Parse("1.2.3.4"), 1234),
            tcp => tcp
                .WithProtocolType(ProtocolType.IcmpV6)
                .WithAddressFamily(AddressFamily.InterNetworkV6));

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        MqttClientTcpConfiguration tcpConfiguration = configuration.Channel.ShouldBeOfType<MqttClientTcpConfiguration>();
        tcpConfiguration.RemoteEndpoint.ShouldBe(new IPEndPoint(IPAddress.Parse("1.2.3.4"), 1234));
        tcpConfiguration.ProtocolType.ShouldBe(ProtocolType.IcmpV6);
        tcpConfiguration.AddressFamily.ShouldBe(AddressFamily.InterNetworkV6);
    }

    [Fact]
    public void ConnectViaTcp_ShouldSetChannelFromBuilderAction()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.ConnectViaTcp(tcp => tcp.WithRemoteEndpoint(new IPEndPoint(42, 1234)));

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        MergeableActionCollection<MqttClientConfigurationBuilder> mergeableActionCollection = builder.GetConfigurationActions();
        mergeableActionCollection.Count.ShouldBe(1);
        mergeableActionCollection.ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        MqttClientTcpConfiguration tcpConfiguration = configuration.Channel.ShouldBeOfType<MqttClientTcpConfiguration>();
        tcpConfiguration.RemoteEndpoint.ShouldBe(new IPEndPoint(42, 1234));
    }

    [Fact]
    public void ConnectViaWebSocket_ShouldSetChannelFromUri()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.ConnectViaWebSocket("uri");

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        MqttClientWebSocketConfiguration webSocketConfiguration = configuration.Channel.ShouldBeOfType<MqttClientWebSocketConfiguration>();
        webSocketConfiguration.Uri.ShouldBe("uri");
    }

    [Fact]
    public void ConnectViaWebSocket_ShouldSetChannelFromUriAndBuilderAction()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.ConnectViaWebSocket("uri", webSocket => webSocket.UseProxy("proxy"));

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        MqttClientWebSocketConfiguration webSocketConfiguration = configuration.Channel.ShouldBeOfType<MqttClientWebSocketConfiguration>();
        webSocketConfiguration.Uri.ShouldBe("uri");
        webSocketConfiguration.Proxy.ShouldNotBeNull();
        webSocketConfiguration.Proxy.Address.ShouldBe("proxy");
    }

    [Fact]
    public void ConnectViaWebSocket_ShouldSetChannelFromBuilderAction()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.ConnectViaWebSocket(webSocket => webSocket.WithUri("uri"));

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();

        MqttClientWebSocketConfiguration webSocketConfiguration = configuration.Channel.ShouldBeOfType<MqttClientWebSocketConfiguration>();
        webSocketConfiguration.Uri.ShouldBe("uri");
    }

    [Fact]
    public void EnableTls_ShouldSetTlsConfiguration()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder
            .ConnectViaTcp("tests-server")
            .EnableTls(tls => tls
                .WithSslProtocol(SslProtocols.Tls12)
                .AllowUntrustedCertificates()
                .IgnoreCertificateChainErrors()
                .IgnoreCertificateRevocationErrors());

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();
        MqttClientTcpConfiguration tcpConfiguration = configuration.Channel.ShouldBeOfType<MqttClientTcpConfiguration>();
        tcpConfiguration.Tls.UseTls.ShouldBeTrue();
        tcpConfiguration.Tls.SslProtocol.ShouldBe(SslProtocols.Tls12);
        tcpConfiguration.Tls.AllowUntrustedCertificates.ShouldBeTrue();
        tcpConfiguration.Tls.IgnoreCertificateChainErrors.ShouldBeTrue();
        tcpConfiguration.Tls.IgnoreCertificateRevocationErrors.ShouldBeTrue();
    }

    [Fact]
    public void EnableTls_ShouldSetUseTls_WhenConnectingViaWebSocket()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder
            .ConnectViaWebSocket("tests-server")
            .EnableTls();

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();
        MqttClientWebSocketConfiguration webSocketConfiguration = configuration.Channel.ShouldBeOfType<MqttClientWebSocketConfiguration>();
        webSocketConfiguration.Tls.UseTls.ShouldBeTrue();
    }

    [Fact]
    public void DisableTls_ShouldSetUseTls_WhenConnectingViaWebSocket()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder
            .ConnectViaWebSocket("tests-server")
            .DisableTls();

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();
        MqttClientWebSocketConfiguration webSocketConfiguration = configuration.Channel.ShouldBeOfType<MqttClientWebSocketConfiguration>();
        webSocketConfiguration.Tls.UseTls.ShouldBeFalse();
    }

    [Fact]
    public void EnableTls_ShouldSetUseTls_WhenConnectingViaTcp()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder
            .ConnectViaTcp("tests-server")
            .EnableTls();

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();
        MqttClientTcpConfiguration tcpConfiguration = configuration.Channel.ShouldBeOfType<MqttClientTcpConfiguration>();
        tcpConfiguration.Tls.UseTls.ShouldBeTrue();
    }

    [Fact]
    public void DisableTls_ShouldSetUseTls_WhenConnectingViaTcp()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder
            .ConnectViaTcp("tests-server")
            .DisableTls();

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();
        MqttClientTcpConfiguration tcpConfiguration = configuration.Channel.ShouldBeOfType<MqttClientTcpConfiguration>();
        tcpConfiguration.Tls.UseTls.ShouldBeFalse();
    }

    [Fact]
    public void EnableParallelProcessing_ShouldSetMaxDegreeOfParallelism()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.EnableParallelProcessing(42);

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();
        configuration.MaxDegreeOfParallelism.ShouldBe(42);
    }

    [Fact]
    public void DisableParallelProcessing_ShouldSetMaxDegreeOfParallelism()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.DisableParallelProcessing();

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();
        configuration.MaxDegreeOfParallelism.ShouldBe(1);
    }

    [Fact]
    public void LimitBackpressure_ShouldSetBackpressureLimit()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.LimitBackpressure(42);

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();
        configuration.BackpressureLimit.ShouldBe(42);
    }

    [Fact]
    public void WithAcknowledgmentTimeout_ShouldSetAcknowledgmentTimeout()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.WithAcknowledgmentTimeout(TimeSpan.FromSeconds(42));

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();
        configuration.AcknowledgmentTimeout.ShouldBe(TimeSpan.FromSeconds(42));
    }

    [Fact]
    public void EnableTryPrivate_ShouldSetTryPrivate()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.EnableTryPrivate();

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();
        configuration.TryPrivate.ShouldBeTrue();
    }

    [Fact]
    public void DisableTryPrivate_ShouldSetTryPrivate()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.DisableTryPrivate();

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();
        configuration.TryPrivate.ShouldBeFalse();
    }

    [Fact]
    public void RequestCleanStart_ShouldSetCleanSession()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.RequestCleanStart();

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();
        configuration.CleanSession.ShouldBeTrue();
    }

    [Fact]
    public void AllowPacketFragmentation_ShouldSetAllowPacketFragmentationTrue()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.AllowPacketFragmentation();

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();
        configuration.AllowPacketFragmentation.ShouldBeTrue();
    }

    [Fact]
    public void DisablePacketFragmentation_ShouldSetAllowPacketFragmentationFalse()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.DisablePacketFragmentation();

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration configuration = clientConfigurationBuilder.Build();
        configuration.AllowPacketFragmentation.ShouldBeFalse();
    }

    [Fact]
    public async Task AddClient_ShouldAddClients()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(broker => broker.AddMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("test", 42)
                .AddClient(
                    "client1",
                    client => client
                        .Consume(endpoint => endpoint.ConsumeFrom("topic1"))
                        .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic2")))
                .AddClient(
                    "client2",
                    client => client
                        .Consume(endpoint => endpoint.ConsumeFrom("topic3"))
                        .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo("topic4")))));

        await serviceProvider.GetRequiredService<BrokerClientsBootstrapper>().InitializeAllAsync();

        ProducerCollection producers = serviceProvider.GetRequiredService<ProducerCollection>();
        producers.Count.ShouldBe(2);
        MqttProducer producer1 = producers.GetProducerForEndpoint("topic2").ShouldBeOfType<MqttProducer>();
        producer1.EndpointConfiguration.MessageType.ShouldBe(typeof(TestEventOne));
        producer1.EndpointConfiguration.EndpointResolver.RawName.ShouldBe("topic2");
        MqttProducer producer2 = producers.GetProducerForEndpoint("topic4").ShouldBeOfType<MqttProducer>();
        producer2.EndpointConfiguration.MessageType.ShouldBe(typeof(TestEventTwo));
        producer2.EndpointConfiguration.EndpointResolver.RawName.ShouldBe("topic4");
        ConsumerCollection consumers = serviceProvider.GetRequiredService<ConsumerCollection>();
        consumers.Count.ShouldBe(2);
        MqttConsumer consumer1 = consumers["client1"].ShouldBeOfType<MqttConsumer>();
        consumer1.Configuration.ConsumerEndpoints.Count.ShouldBe(1);
        consumer1.Configuration.ConsumerEndpoints.First().RawName.ShouldBe("topic1");
        MqttConsumer consumer2 = consumers["client2"].ShouldBeOfType<MqttConsumer>();
        consumer2.Configuration.ConsumerEndpoints.Count.ShouldBe(1);
        consumer2.Configuration.ConsumerEndpoints.First().RawName.ShouldBe("topic3");
    }

    [Fact]
    public async Task AddProducer_ShouldMergeProducerConfiguration_WhenNameIsTheSame()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(broker => broker.AddMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("test", 42)
                .AddClient(
                    "client1",
                    client => client
                        .WithClientId("client42")
                        .Consume(endpoint => endpoint.ConsumeFrom("topic1"))
                        .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic2")))
                .AddClient(
                    "client1",
                    client => client
                        .LimitPacketSize(42)
                        .Consume(endpoint => endpoint.ConsumeFrom("topic3"))
                        .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo("topic4")))));

        await serviceProvider.GetRequiredService<BrokerClientsBootstrapper>().InitializeAllAsync();

        ProducerCollection producers = serviceProvider.GetRequiredService<ProducerCollection>();
        producers.Count.ShouldBe(2);
        MqttProducer producer1 = producers.GetProducerForEndpoint("topic2").ShouldBeOfType<MqttProducer>();
        producer1.EndpointConfiguration.MessageType.ShouldBe(typeof(TestEventOne));
        producer1.EndpointConfiguration.EndpointResolver.RawName.ShouldBe("topic2");
        producer1.Configuration.ClientId.ShouldBe("client42");
        producer1.Configuration.MaximumPacketSize.ShouldBe((ushort)42);
        MqttProducer producer2 = producers.GetProducerForEndpoint("topic4").ShouldBeOfType<MqttProducer>();
        producer2.EndpointConfiguration.MessageType.ShouldBe(typeof(TestEventTwo));
        producer2.EndpointConfiguration.EndpointResolver.RawName.ShouldBe("topic4");
        producer2.Configuration.ClientId.ShouldBe("client42");
        producer2.Configuration.MaximumPacketSize.ShouldBe((ushort)42);
        producer2.Client.ShouldBeSameAs(producer1.Client);

        ConsumerCollection consumers = serviceProvider.GetRequiredService<ConsumerCollection>();
        consumers.Count.ShouldBe(1);
        MqttConsumer consumer = consumers["client1"].ShouldBeOfType<MqttConsumer>();
        consumer.Configuration.ConsumerEndpoints.Count.ShouldBe(2);
        consumer.Configuration.ConsumerEndpoints.First().RawName.ShouldBe("topic1");
        consumer.Configuration.ConsumerEndpoints.Last().RawName.ShouldBe("topic3");
        consumer.Configuration.ClientId.ShouldBe("client42");
        consumer.Configuration.MaximumPacketSize.ShouldBe((ushort)42);
    }

    private static MqttClientsConfigurationBuilder GetBuilder()
    {
        MqttClientsConfigurationBuilder builder = new();
        builder.AddClient(_ =>
        {
        });
        return builder;
    }

    private static MqttClientConfigurationBuilder GetClientConfigurationBuilderWithValidConfigurationAndEndpoint(IServiceProvider? serviceProvider = null) =>
        GetClientConfigurationBuilderWithValidConfiguration(serviceProvider)
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic"));

    private static MqttClientConfigurationBuilder GetClientConfigurationBuilderWithValidConfiguration(IServiceProvider? serviceProvider = null) =>
        new MqttClientConfigurationBuilder(serviceProvider ?? Substitute.For<IServiceProvider>())
            .ConnectViaTcp("test");

    private sealed class TestEnhancedAuthenticationHandler : IMqttEnhancedAuthenticationHandler
    {
        public Task HandleEnhancedAuthenticationAsync(MqttEnhancedAuthenticationEventArgs eventArgs) => Task.CompletedTask;
    }
}
