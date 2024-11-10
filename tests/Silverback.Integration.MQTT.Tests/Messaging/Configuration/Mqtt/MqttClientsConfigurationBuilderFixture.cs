// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Client;
using MQTTnet.Formatter;
using NSubstitute;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Tests.Logging;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public class MqttClientsConfigurationBuilderFixture
{
    [Fact]
    public void UseProtocolVersion_ShouldSetProtocolVersion()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.UseProtocolVersion(MqttProtocolVersion.V311);

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.ProtocolVersion.Should().Be(MqttProtocolVersion.V311);
    }

    [Fact]
    public void WithTimeout_ShouldSetTimeout()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.WithTimeout(TimeSpan.FromSeconds(42));

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.Timeout.TotalSeconds.Should().Be(42);
    }

    [Fact]
    public void RequestCleanSession_ShouldSetCleanSession()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.RequestCleanSession();

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.CleanSession.Should().BeTrue();
    }

    [Fact]
    public void RequestPersistentSession_ShouldSetCleanSession()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.RequestPersistentSession();

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.CleanSession.Should().BeFalse();
    }

    [Fact]
    public void DisableKeepAlive_ShouldSetKeepAliveInterval()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.DisableKeepAlive();

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.KeepAlivePeriod.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void SendKeepAlive_ShouldSetKeepAliveInterval()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.SendKeepAlive(TimeSpan.FromMinutes(42));

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.KeepAlivePeriod.Should().Be(TimeSpan.FromMinutes(42));
    }

    [Fact]
    public void SendLastWillMessage_ShouldSetWillMessageAndDelay()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.SendLastWillMessage<TestEventOne>(
            lastWill => lastWill
                .SendMessage(
                    new TestEventOne
                    {
                        Content = "I died!"
                    })
                .WithDelay(TimeSpan.FromSeconds(42))
                .ProduceTo("testaments"));

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.WillMessage.ShouldNotBeNull();
        config.WillMessage.Topic.Should().Be("testaments");
        config.WillMessage.Payload.Should().NotBeNullOrEmpty();
        config.WillMessage.Delay.Should().Be(42);
    }

    [Fact]
    public void WithAuthentication_ShouldSetAuthenticationMethodAndData()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.WithAuthentication("method", [0x01, 0x02, 0x03]);

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.AuthenticationMethod.Should().Be("method");
        config.AuthenticationData.Should().BeEquivalentTo(new byte[] { 0x01, 0x02, 0x03 });
    }

    [Fact]
    public void LimitTopicAlias_ShouldSetTopicAliasMaximum()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.LimitTopicAlias(42);

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.TopicAliasMaximum.Should().Be(42);
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
            act.Should().NotThrow();
        else
            act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void LimitPacketSize_ShouldSetMaximumPacketSize()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.LimitPacketSize(42);

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

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
    public void LimitPacketSize_ShouldValidateRange(long value, bool isValid)
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        Action act = () => builder.LimitPacketSize(value);

        if (isValid)
            act.Should().NotThrow();
        else
            act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void LimitUnacknowledgedPublications_ShouldSetReceiveMaximum()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.LimitUnacknowledgedPublications(42);

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

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
    public void LimitUnacknowledgedPublications_ShouldValidateRange(int value, bool isValid)
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        Action act = () => builder.LimitUnacknowledgedPublications(value);

        if (isValid)
            act.Should().NotThrow();
        else
            act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RequestProblemInformation_ShouldSetRequestProblemInformation()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.RequestProblemInformation();

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.RequestProblemInformation.Should().BeTrue();
    }

    [Fact]
    public void DisableProblemInformation_ShouldSetRequestProblemInformation()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.DisableProblemInformation();

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.RequestProblemInformation.Should().BeFalse();
    }

    [Fact]
    public void RequestResponseInformation_ShouldSetRequestResponseInformation()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.RequestResponseInformation();

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.RequestResponseInformation.Should().BeTrue();
    }

    [Fact]
    public void DisableResponseInformation_ShouldSetRequestResponseInformation()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.DisableResponseInformation();

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.RequestResponseInformation.Should().BeFalse();
    }

    [Fact]
    public void WithSessionExpiration_ShouldSetSessionExpiryInterval()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.WithSessionExpiration(TimeSpan.FromSeconds(42));

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.SessionExpiryInterval.Should().Be(42);
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
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.UserProperties.Should().BeEquivalentTo(
            new[]
            {
                new MqttUserProperty("prop1", "value1"),
                new MqttUserProperty("prop2", "value2")
            });
    }

    [Fact]
    public void WithCredentials_ShouldSetCredentialsWithStringPassword()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.WithCredentials("user", "pass");

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.Credentials.ShouldNotBeNull();
        config.Credentials.GetUserName(config.GetMqttClientOptions()).Should().Be("user");
        config.Credentials.GetPassword(config.GetMqttClientOptions()).Should().BeEquivalentTo(Encoding.UTF8.GetBytes("pass"));
    }

    [Fact]
    public void WithCredentials_ShouldSetCredentialsWithBytesPassword()
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes("pass");
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.WithCredentials("user", passwordBytes);

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.Credentials.ShouldNotBeNull();
        config.Credentials.GetUserName(config.GetMqttClientOptions()).Should().Be("user");
        config.Credentials.GetPassword(config.GetMqttClientOptions()).Should().BeEquivalentTo(passwordBytes);
    }

    [Fact]
    public void WithCredentials_ShouldSetCredentialsFromClientCredentialsProvider()
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes("pass");
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.WithCredentials(new MqttClientCredentials("user", passwordBytes));

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.Credentials.ShouldNotBeNull();
        config.Credentials.GetUserName(config.GetMqttClientOptions()).Should().Be("user");
        config.Credentials.GetPassword(config.GetMqttClientOptions()).Should().BeEquivalentTo(passwordBytes);
    }

    [Fact]
    public void UseExtendedAuthenticationExchangeHandler_ShouldSetHandlerFromInstance()
    {
        TestExtendedAuthenticationExchangeHandler instance = new();
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.UseExtendedAuthenticationExchangeHandler(instance);

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.ExtendedAuthenticationExchangeHandler.Should().BeSameAs(instance);
    }

    [Fact]
    public void UseExtendedAuthenticationExchangeHandler_ShouldSetHandlerFromGenericTypeArgument()
    {
        IServiceProvider? serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(TestExtendedAuthenticationExchangeHandler))
            .Returns(new TestExtendedAuthenticationExchangeHandler());

        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder
            .ConnectViaTcp("tests-server")
            .UseExtendedAuthenticationExchangeHandler<TestExtendedAuthenticationExchangeHandler>();

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint(serviceProvider);
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.ExtendedAuthenticationExchangeHandler.Should().BeOfType<TestExtendedAuthenticationExchangeHandler>();
    }

    [Fact]
    public void UseExtendedAuthenticationExchangeHandler_ShouldSetHandlerFromType()
    {
        IServiceProvider? serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(TestExtendedAuthenticationExchangeHandler))
            .Returns(new TestExtendedAuthenticationExchangeHandler());

        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.UseExtendedAuthenticationExchangeHandler(typeof(TestExtendedAuthenticationExchangeHandler));

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint(serviceProvider);
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.ExtendedAuthenticationExchangeHandler.Should().BeOfType<TestExtendedAuthenticationExchangeHandler>();
    }

    [Fact]
    public void ConnectTo_ShouldConnectViaTcp()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.ConnectTo("mqtt://test:42");

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.Channel.Should().BeOfType<MqttClientTcpConfiguration>();
        config.Channel.As<MqttClientTcpConfiguration>().RemoteEndpoint.Should().BeEquivalentTo(new DnsEndPoint("test", 42));
    }

    [Fact]
    public void ConnectViaTcp_ShouldSetChannelFromServerAndPort()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.ConnectViaTcp("tests-server", 1234);

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.Channel.Should().BeOfType<MqttClientTcpConfiguration>();
        config.Channel.As<MqttClientTcpConfiguration>().RemoteEndpoint.Should().BeEquivalentTo(new DnsEndPoint("tests-server", 1234));
    }

    [Fact]
    public void ConnectViaTcp_ShouldSetChannel()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.ConnectViaTcp(
            new MqttClientTcpConfiguration
            {
                RemoteEndpoint = new DnsEndPoint("tests-server", 1234),
            });

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        MergeableActionCollection<MqttClientConfigurationBuilder> mergeableActionCollection = builder.GetConfigurationActions();
        mergeableActionCollection.Should().HaveCount(1);
        mergeableActionCollection.ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.Channel.Should().BeOfType<MqttClientTcpConfiguration>();
        config.Channel.As<MqttClientTcpConfiguration>().RemoteEndpoint.Should().BeEquivalentTo(new DnsEndPoint("tests-server", 1234));
    }

    [Fact]
    public void ConnectViaWebSocket_ShouldSetChannelFromUri()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.ConnectViaWebSocket("uri");

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.Channel.Should().BeOfType<MqttClientWebSocketConfiguration>();
        config.Channel.As<MqttClientWebSocketConfiguration>().Uri.Should().Be("uri");
    }

    [Fact]
    public void ConnectViaWebSocket_ShouldSetChannel()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder.ConnectViaWebSocket(
            new MqttClientWebSocketConfiguration
            {
                Uri = "uri",
                RequestHeaders = new Dictionary<string, string>
                {
                    { "header", "value" }
                }
            });

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.Channel.Should().BeOfType<MqttClientWebSocketConfiguration>();
        config.Channel.As<MqttClientWebSocketConfiguration>().Uri.Should().Be("uri");
        config.Channel.As<MqttClientWebSocketConfiguration>().RequestHeaders.Should().BeEquivalentTo(
            new Dictionary<string, string>
            {
                { "header", "value" }
            });
    }

    [Fact]
    public void UseProxy_ShouldSetProxyFromAddressEtc()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder
            .ConnectViaWebSocket("uri")
            .UseProxy(
                "address",
                "user",
                "pass",
                "domain",
                true,
                ["local1", "local2"]);

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.Channel.As<MqttClientWebSocketConfiguration>().Proxy.Should().NotBeNull();
        config.Channel.As<MqttClientWebSocketConfiguration>().Proxy!.Address.Should().Be("address");
        config.Channel.As<MqttClientWebSocketConfiguration>().Proxy!.Username.Should().Be("user");
        config.Channel.As<MqttClientWebSocketConfiguration>().Proxy!.Password.Should().Be("pass");
        config.Channel.As<MqttClientWebSocketConfiguration>().Proxy!.Domain.Should().Be("domain");
        config.Channel.As<MqttClientWebSocketConfiguration>().Proxy!.BypassOnLocal.Should().BeTrue();
        config.Channel.As<MqttClientWebSocketConfiguration>().Proxy!.BypassList.Should().BeEquivalentTo("local1", "local2");
    }

    [Fact]
    public void UseProxy_ShouldSetProxy()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

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
                    BypassList = ["local1", "local2"]
                });

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.Channel.As<MqttClientWebSocketConfiguration>().Proxy.Should().NotBeNull();
        config.Channel.As<MqttClientWebSocketConfiguration>().Proxy!.Address.Should().Be("address");
        config.Channel.As<MqttClientWebSocketConfiguration>().Proxy!.Username.Should().Be("user");
        config.Channel.As<MqttClientWebSocketConfiguration>().Proxy!.Password.Should().Be("pass");
        config.Channel.As<MqttClientWebSocketConfiguration>().Proxy!.Domain.Should().Be("domain");
        config.Channel.As<MqttClientWebSocketConfiguration>().Proxy!.BypassOnLocal.Should().BeTrue();
        config.Channel.As<MqttClientWebSocketConfiguration>().Proxy!.BypassList.Should().BeEquivalentTo("local1", "local2");
    }

    [Fact]
    public void EnableTls_ShouldSetUseTls()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder
            .ConnectViaTcp("tests-server")
            .EnableTls();

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.Channel.As<MqttClientTcpConfiguration>().Tls.UseTls.Should().BeTrue();
    }

    [Fact]
    public void DisableTls_ShouldSetUseTls()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder
            .ConnectViaTcp("tests-server")
            .DisableTls();

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.Channel.As<MqttClientTcpConfiguration>().Tls.UseTls.Should().BeFalse();
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
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.Channel.As<MqttClientWebSocketConfiguration>().Tls.UseTls.Should().BeTrue();
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
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.Channel.As<MqttClientWebSocketConfiguration>().Tls.UseTls.Should().BeFalse();
    }

    [Fact]
    public void EnableTls_ShouldSetUseTls_WhenConnectingViaTcp()
    {
        MqttClientsConfigurationBuilder builder = GetBuilder();

        builder
            .ConnectViaTcp("tests-server")
            .EnableTls(
                new MqttClientTlsConfiguration
                {
                    UseTls = true,
                    SslProtocol = SslProtocols.Tls12,
                    AllowUntrustedCertificates = true
                });

        MqttClientConfigurationBuilder clientConfigurationBuilder = GetClientConfigurationBuilderWithValidConfigurationAndEndpoint();
        builder.GetConfigurationActions().ForEach(action => action.Action.Invoke(clientConfigurationBuilder));
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.Channel.As<MqttClientTcpConfiguration>().Tls.UseTls.Should().BeTrue();
        config.Channel.As<MqttClientTcpConfiguration>().Tls.SslProtocol.Should().Be(SslProtocols.Tls12);
        config.Channel.As<MqttClientTcpConfiguration>().Tls.AllowUntrustedCertificates.Should().BeTrue();
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
        MqttClientConfiguration config = clientConfigurationBuilder.Build();

        config.Channel.As<MqttClientTcpConfiguration>().Tls.UseTls.Should().BeFalse();
    }

    [Fact]
    public async Task AddClient_ShouldAddClients()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(broker => broker.AddMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("test", 42)
                        .AddClient(
                            client => client
                                .Consume(endpoint => endpoint.ConsumeFrom("topic1"))
                                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic2")))
                        .AddClient(
                            client => client
                                .Consume(endpoint => endpoint.ConsumeFrom("topic3"))
                                .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo("topic4")))));

        await serviceProvider.GetRequiredService<BrokerClientsBootstrapper>().InitializeAllAsync();

        ProducerCollection producers = serviceProvider.GetRequiredService<ProducerCollection>();
        producers.Should().HaveCount(2);
        producers[0].EndpointConfiguration.MessageType.Should().Be<TestEventOne>();
        producers[0].EndpointConfiguration.As<MqttProducerEndpointConfiguration>().EndpointResolver.RawName.Should().Be("topic2");
        producers[1].EndpointConfiguration.MessageType.Should().Be<TestEventTwo>();
        producers[1].EndpointConfiguration.As<MqttProducerEndpointConfiguration>().EndpointResolver.RawName.Should().Be("topic4");
        ConsumerCollection consumers = serviceProvider.GetRequiredService<ConsumerCollection>();
        consumers.Should().HaveCount(2);
        consumers[0].As<MqttConsumer>().Configuration.ConsumerEndpoints.Should().HaveCount(1);
        consumers[0].As<MqttConsumer>().Configuration.ConsumerEndpoints.First().RawName.Should().Be("topic1");
        consumers[1].As<MqttConsumer>().Configuration.ConsumerEndpoints.Should().HaveCount(1);
        consumers[1].As<MqttConsumer>().Configuration.ConsumerEndpoints.First().RawName.Should().Be("topic3");
    }

    [Fact]
    public async Task AddProducer_ShouldMergeProducerConfiguration_WhenIdIsTheSame()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(broker => broker.AddMqtt())
                .AddMqttClients(
                    clients => clients
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

        MqttProducer[] producers = serviceProvider.GetRequiredService<ProducerCollection>().Cast<MqttProducer>().ToArray();
        producers.Should().HaveCount(2);
        producers[0].EndpointConfiguration.MessageType.Should().Be<TestEventOne>();
        producers[0].EndpointConfiguration.As<MqttProducerEndpointConfiguration>().EndpointResolver.RawName.Should().Be("topic2");
        producers[0].As<MqttProducer>().Configuration.ClientId.Should().Be("client42");
        producers[0].As<MqttProducer>().Configuration.MaximumPacketSize.Should().Be(42);
        producers[1].EndpointConfiguration.MessageType.Should().Be<TestEventTwo>();
        producers[1].EndpointConfiguration.As<MqttProducerEndpointConfiguration>().EndpointResolver.RawName.Should().Be("topic4");
        producers[1].As<MqttProducer>().Configuration.ClientId.Should().Be("client42");
        producers[1].As<MqttProducer>().Configuration.MaximumPacketSize.Should().Be(42);
        producers[1].Client.Should().BeSameAs(producers[0].Client);

        ConsumerCollection consumers = serviceProvider.GetRequiredService<ConsumerCollection>();
        consumers.Should().HaveCount(1);
        consumers[0].As<MqttConsumer>().Configuration.ConsumerEndpoints.Should().HaveCount(2);
        consumers[0].As<MqttConsumer>().Configuration.ConsumerEndpoints.First().RawName.Should().Be("topic1");
        consumers[0].As<MqttConsumer>().Configuration.ConsumerEndpoints.Last().RawName.Should().Be("topic3");
        consumers[0].As<MqttConsumer>().Configuration.ClientId.Should().Be("client42");
        consumers[0].As<MqttConsumer>().Configuration.MaximumPacketSize.Should().Be(42);
    }

    private static MqttClientsConfigurationBuilder GetBuilder()
    {
        MqttClientsConfigurationBuilder builder = new();
        builder.AddClient(
            _ =>
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

    private sealed class TestExtendedAuthenticationExchangeHandler : IMqttExtendedAuthenticationExchangeHandler
    {
        public Task HandleRequestAsync(MqttExtendedAuthenticationExchangeContext context) => Task.CompletedTask;
    }

    // TODO: Reimplement (here or somewhere else, and for Kafka as well)
    // [Fact]
    // public async Task AddInboundAddOutbound_MultipleConfiguratorsWithInvalidEndpoints_ValidEndpointsAdded()
    // {
    //     IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
    //         services => services
    //             .AddFakeLogger()
    //             .AddSilverback()
    //             .WithConnectionToMessageBroker(broker => broker.AddMqtt())
    //             .AddMqttEndpoints(
    //                 endpoints => endpoints
    //                     .ConfigureClient(
    //                         clientConfiguration => clientConfiguration
    //                             .WithBootstrapServers("PLAINTEXT://unittest"))
    //                     .AddOutbound<TestEventOne>(
    //                         endpoint => endpoint
    //                             .ProduceTo("test1"))
    //                     .AddInbound(
    //                         consumer => consumer
    //                             .ConfigureClient(clientConfiguration => clientConfiguration.WithGroupId("group1"))
    //                             .ConsumeFrom(string.Empty))
    //                     .AddInbound(
    //                         consumer => consumer
    //                             .ConfigureClient(clientConfiguration => clientConfiguration.WithGroupId("group2"))
    //                             .ConsumeFrom("test1")))
    //             .AddMqttEndpoints(_ => throw new InvalidOperationException())
    //             .AddMqttEndpoints(
    //                 endpoints => endpoints
    //                     .ConfigureClient(
    //                         clientConfiguration => clientConfiguration
    //                             .WithBootstrapServers("PLAINTEXT://unittest"))
    //                     .AddOutbound<TestEventOne>(endpoint => endpoint.ProduceTo(string.Empty))
    //                     .AddOutbound<TestEventOne>(endpoint => endpoint.ProduceTo("test2"))
    //                     .AddInbound(
    //                         consumer => consumer
    //                             .ConfigureClient(clientConfiguration => clientConfiguration.WithGroupId("group1"))
    //                             .ConsumeFrom("test2"))));
    //
    //     MqttBroker broker = serviceProvider.GetRequiredService<MqttBroker>();
    //     await broker.ConnectAsync();
    //
    //     broker.Producers.Should().HaveCount(2);
    //     broker.Producers[0].Configuration.RawName.Should().Be("test1");
    //     broker.Producers[1].Configuration.RawName.Should().Be("test2");
    //     broker.Consumers.Should().HaveCount(2);
    //     broker.Consumers[0].Configuration.RawName.Should().Be("test1");
    //     broker.Consumers[1].Configuration.RawName.Should().Be("test2");
    // }
}
