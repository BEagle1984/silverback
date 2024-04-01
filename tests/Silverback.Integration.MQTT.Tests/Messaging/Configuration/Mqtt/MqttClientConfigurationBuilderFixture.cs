// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading.Tasks;
using FluentAssertions;
using MQTTnet.Client;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using NSubstitute;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public class MqttClientConfigurationBuilderFixture
{
    [Fact]
    public void Constructor_ShouldSetProtocolVersionToV500()
    {
        MqttClientConfigurationBuilder builder = new();

        builder
            .ConnectViaTcp("tests-server")
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic"));

        MqttClientConfiguration config = builder.Build();
        config.ProtocolVersion.Should().Be(MqttProtocolVersion.V500);
    }

    [Fact]
    public void Build_ShouldThrow_WhenConfigurationIsNotValid()
    {
        MqttClientConfigurationBuilder builder = new();

        Action act = () => builder.Build();

        act.Should().Throw<SilverbackConfigurationException>();
    }

    [Fact]
    public void Build_ShouldReturnNewConfiguration_WhenReused()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.WithClientId("one");
        MqttClientConfiguration configuration1 = builder.Build();

        builder.WithClientId("two");
        MqttClientConfiguration configuration2 = builder.Build();

        configuration1.ClientId.Should().Be("one");
        configuration2.ClientId.Should().Be("two");
    }

    [Fact]
    public void Produce_ShouldAddEndpoints()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1"))
            .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo("topic2"));

        MqttClientConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.ProducerEndpoints.Should().HaveCount(2);
        MqttProducerEndpointConfiguration endpoint1 = configuration.ProducerEndpoints.First();
        endpoint1.Endpoint.As<MqttStaticProducerEndpointResolver>().Topic.Should().Be("topic1");
        endpoint1.Serializer.Should().BeOfType<JsonMessageSerializer>();
        MqttProducerEndpointConfiguration endpoint2 = configuration.ProducerEndpoints.Skip(1).First();
        endpoint2.Endpoint.As<MqttStaticProducerEndpointResolver>().Topic.Should().Be("topic2");
        endpoint2.Serializer.Should().BeOfType<JsonMessageSerializer>();
    }

    [Fact]
    public void Produce_ShouldAddEndpointWithGenericMessageType_WhenNoTypeIsSpecified()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder.Produce(endpoint => endpoint.ProduceTo("topic1"));

        MqttClientConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.ProducerEndpoints.Should().HaveCount(1);
        MqttProducerEndpointConfiguration endpoint = configuration.ProducerEndpoints.Single();
        endpoint.MessageType.Should().Be<object>();
        endpoint.Endpoint.As<MqttStaticProducerEndpointResolver>().Topic.Should().Be("topic1");
        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer>();
    }

    [Fact]
    public void Produce_ShouldIgnoreEndpointsWithSameId()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Produce<TestEventOne>("id1", endpoint => endpoint.ProduceTo("topic1"))
            .Produce<TestEventTwo>("id1", endpoint => endpoint.ProduceTo("topic1"));

        MqttClientConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.ProducerEndpoints.Should().HaveCount(1);
        MqttProducerEndpointConfiguration endpoint1 = configuration.ProducerEndpoints.First();
        endpoint1.Endpoint.As<MqttStaticProducerEndpointResolver>().Topic.Should().Be("topic1");
        endpoint1.Serializer.Should().BeOfType<JsonMessageSerializer>();
    }

    [Fact]
    public void Produce_ShouldIgnoreEndpointsWithSameTopicNameAndMessageTypeAndNoId()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1").WithExactlyOnceQoS())
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1").WithAtLeastOnceQoS());

        MqttClientConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.ProducerEndpoints.Should().HaveCount(1);
        MqttProducerEndpointConfiguration endpoint1 = configuration.ProducerEndpoints.First();
        endpoint1.Endpoint.As<MqttStaticProducerEndpointResolver>().Topic.Should().Be("topic1");
        endpoint1.Serializer.Should().BeOfType<JsonMessageSerializer>();
        endpoint1.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.ExactlyOnce);
    }

    [Fact]
    public void Produce_ShouldAddEndpointsWithSameTopicNameAndDifferentMessageType()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1"))
            .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo("topic1"));

        MqttClientConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.ProducerEndpoints.Should().HaveCount(2);
    }

    [Fact]
    public void Produce_ShouldAddEndpointsWithSameTopicNameAndDifferentId()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Produce<TestEventOne>("id1", endpoint => endpoint.ProduceTo("topic1"))
            .Produce<TestEventTwo>("id2", endpoint => endpoint.ProduceTo("topic1"));

        MqttClientConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.ProducerEndpoints.Should().HaveCount(2);
    }

    [Fact]
    public void Consume_ShouldAddEndpointsWithoutMessageType()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Consume(endpoint => endpoint.ConsumeFrom("topic1"))
            .Consume(endpoint => endpoint.ConsumeFrom("topic2"));

        MqttClientConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.ConsumerEndpoints.Should().HaveCount(2);
        MqttConsumerEndpointConfiguration endpoint1 = configuration.ConsumerEndpoints.First();
        endpoint1.Topics.Should().HaveCount(1);
        endpoint1.Topics.First().Should().Be("topic1");
        endpoint1.Deserializer.Should().BeOfType<JsonMessageDeserializer<object>>();
        MqttConsumerEndpointConfiguration endpoint2 = configuration.ConsumerEndpoints.Skip(1).First();
        endpoint2.Topics.Should().HaveCount(1);
        endpoint2.Topics.First().Should().Be("topic2");
        endpoint2.Deserializer.Should().BeOfType<JsonMessageDeserializer<object>>();
    }

    [Fact]
    public void Consume_ShouldAddEndpointsForMessageType()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom("topic1"))
            .Consume<TestEventTwo>(endpoint => endpoint.ConsumeFrom("topic2"));

        MqttClientConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.ConsumerEndpoints.Should().HaveCount(2);
        MqttConsumerEndpointConfiguration endpoint1 = configuration.ConsumerEndpoints.First();
        endpoint1.Topics.Should().HaveCount(1);
        endpoint1.Topics.First().Should().Be("topic1");
        endpoint1.Deserializer.Should().BeOfType<JsonMessageDeserializer<TestEventOne>>();
        MqttConsumerEndpointConfiguration endpoint2 = configuration.ConsumerEndpoints.Skip(1).First();
        endpoint2.Topics.Should().HaveCount(1);
        endpoint2.Topics.First().Should().Be("topic2");
        endpoint2.Deserializer.Should().BeOfType<JsonMessageDeserializer<TestEventTwo>>();
    }

    [Fact]
    public void Consume_ShouldIgnoreEndpointsWithSameId()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Consume("id1", endpoint => endpoint.ConsumeFrom("topic1"))
            .Consume<TestEventTwo>("id1", endpoint => endpoint.ConsumeFrom("topic2"));

        MqttClientConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.ConsumerEndpoints.Should().HaveCount(1);
        configuration.ConsumerEndpoints.First().Topics.Should().HaveCount(1);
        configuration.ConsumerEndpoints.First().Topics.Should().BeEquivalentTo("topic1");
        configuration.ConsumerEndpoints.First().Deserializer.Should().BeOfType<JsonMessageDeserializer<object>>();
    }

    [Fact]
    public void Consume_ShouldIgnoreEndpointsWithSameTopicNameAndMessageTypeAndNoId()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom("topic1").WithExactlyOnceQoS())
            .Consume<TestEventTwo>(endpoint => endpoint.ConsumeFrom("topic1").WithAtLeastOnceQoS());

        MqttClientConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.ConsumerEndpoints.Should().HaveCount(1);
        MqttConsumerEndpointConfiguration endpoint1 = configuration.ConsumerEndpoints.First();
        endpoint1.Topics.Should().HaveCount(1);
        endpoint1.Topics.First().Should().Be("topic1");
        endpoint1.Deserializer.Should().BeOfType<JsonMessageDeserializer<TestEventOne>>();
        endpoint1.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.ExactlyOnce);
    }

    [Fact]
    public void Consume_ShouldAddEndpointsWithSameTopicNameAndDifferentId()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Consume("id1", endpoint => endpoint.ConsumeFrom("topic1"))
            .Consume<TestEventTwo>("id2", endpoint => endpoint.ConsumeFrom("topic1"));

        Action act = () => builder.Build();

        act.Should().Throw<SilverbackConfigurationException>()
            .WithMessage("Cannot connect to the same topic in different endpoints in the same consumer.");
    }

    [Fact]
    public void UseProtocolVersion_ShouldSetProtocolVersion()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.UseProtocolVersion(MqttProtocolVersion.V311);

        MqttClientConfiguration config = builder.Build();
        config.ProtocolVersion.Should().Be(MqttProtocolVersion.V311);
    }

    [Fact]
    public void WithTimeout_ShouldSetTimeout()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.WithTimeout(TimeSpan.FromSeconds(42));

        MqttClientConfiguration config = builder.Build();
        config.Timeout.TotalSeconds.Should().Be(42);
    }

    [Fact]
    public void RequestCleanSession_ShouldSetCleanSession()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.RequestCleanSession();

        MqttClientConfiguration config = builder.Build();
        config.CleanSession.Should().BeTrue();
    }

    [Fact]
    public void RequestPersistentSession_ShouldSetCleanSession()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.RequestPersistentSession();

        MqttClientConfiguration config = builder.Build();
        config.CleanSession.Should().BeFalse();
    }

    [Fact]
    public void DisableKeepAlive_ShouldSetKeepAliveInterval()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.DisableKeepAlive();

        MqttClientConfiguration config = builder.Build();
        config.KeepAlivePeriod.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void SendKeepAlive_ShouldSetKeepAliveInterval()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.SendKeepAlive(TimeSpan.FromMinutes(42));

        MqttClientConfiguration config = builder.Build();
        config.KeepAlivePeriod.Should().Be(TimeSpan.FromMinutes(42));
    }

    [Fact]
    public void WithClientId_ShouldSetClientId()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.WithClientId("client-42");

        MqttClientConfiguration config = builder.Build();
        config.ClientId.Should().Be("client-42");
    }

    [Fact]
    public void SendLastWillMessage_ShouldSetWillMessageAndDelay()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.SendLastWillMessage<TestEventOne>(
            lastWill => lastWill
                .Message(
                    new TestEventOne
                    {
                        Content = "I died!"
                    })
                .WithDelay(TimeSpan.FromSeconds(42))
                .ProduceTo("testaments"));

        MqttClientConfiguration config = builder.Build();
        config.WillMessage.ShouldNotBeNull();
        config.WillMessage.Topic.Should().Be("testaments");
        config.WillMessage.Payload.Should().NotBeNullOrEmpty();
        config.WillMessage.Delay.Should().Be(42);
    }

    [Fact]
    public void WithAuthentication_ShouldSetAuthenticationMethodAndData()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.WithAuthentication("method", [0x01, 0x02, 0x03]);

        MqttClientConfiguration config = builder.Build();
        config.AuthenticationMethod.Should().Be("method");
        config.AuthenticationData.Should().BeEquivalentTo(new byte[] { 0x01, 0x02, 0x03 });
    }

    [Fact]
    public void LimitTopicAlias_ShouldSetTopicAliasMaximum()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.LimitTopicAlias(42);

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
    public void LimitTopicAlias_ShouldValidateRange(int value, bool isValid)
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        Action act = () => builder.LimitTopicAlias(value);

        if (isValid)
            act.Should().NotThrow();
        else
            act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void LimitPacketSize_ShouldSetMaximumPacketSize()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.LimitPacketSize(42);

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
    public void LimitPacketSize_ShouldValidateRange(long value, bool isValid)
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        Action act = () => builder.LimitPacketSize(value);

        if (isValid)
            act.Should().NotThrow();
        else
            act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void LimitUnacknowledgedPublications_ShouldSetReceiveMaximum()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.LimitUnacknowledgedPublications(42);

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
    public void LimitUnacknowledgedPublications_ShouldValidateRange(int value, bool isValid)
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        Action act = () => builder.LimitUnacknowledgedPublications(value);

        if (isValid)
            act.Should().NotThrow();
        else
            act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RequestProblemInformation_ShouldSetRequestProblemInformation()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.RequestProblemInformation();

        MqttClientConfiguration config = builder.Build();
        config.RequestProblemInformation.Should().BeTrue();
    }

    [Fact]
    public void DisableProblemInformation_ShouldSetRequestProblemInformation()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.DisableProblemInformation();

        MqttClientConfiguration config = builder.Build();
        config.RequestProblemInformation.Should().BeFalse();
    }

    [Fact]
    public void RequestResponseInformation_ShouldSetRequestResponseInformation()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.RequestResponseInformation();

        MqttClientConfiguration config = builder.Build();
        config.RequestResponseInformation.Should().BeTrue();
    }

    [Fact]
    public void DisableResponseInformation_ShouldSetRequestResponseInformation()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.DisableResponseInformation();

        MqttClientConfiguration config = builder.Build();
        config.RequestResponseInformation.Should().BeFalse();
    }

    [Fact]
    public void WithSessionExpiration_ShouldSetSessionExpiryInterval()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.WithSessionExpiration(TimeSpan.FromSeconds(42));

        MqttClientConfiguration config = builder.Build();
        config.SessionExpiryInterval.Should().Be(42);
    }

    [Fact]
    public void AddUserProperty_ShouldAddUserProperties()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder
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
    public void WithCredentials_ShouldSetCredentialsWithStringPassword()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.WithCredentials("user", "pass");

        MqttClientConfiguration config = builder.Build();
        config.Credentials.ShouldNotBeNull();
        config.Credentials.GetUserName(config.GetMqttClientOptions()).Should().Be("user");
        config.Credentials.GetPassword(config.GetMqttClientOptions()).Should().BeEquivalentTo("pass"u8.ToArray());
    }

    [Fact]
    public void WithCredentials_ShouldSetCredentialsWithBytesPassword()
    {
        byte[] passwordBytes = "pass"u8.ToArray();
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.WithCredentials("user", passwordBytes);

        MqttClientConfiguration config = builder.Build();
        config.Credentials.ShouldNotBeNull();
        config.Credentials.GetUserName(config.GetMqttClientOptions()).Should().Be("user");
        config.Credentials.GetPassword(config.GetMqttClientOptions()).Should().BeEquivalentTo(passwordBytes);
    }

    [Fact]
    public void WithCredentials_ShouldSetCredentialsFromClientCredentials()
    {
        byte[] passwordBytes = "pass"u8.ToArray();
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.WithCredentials(new MqttClientCredentials("user", passwordBytes));

        MqttClientConfiguration config = builder.Build();
        config.Credentials.ShouldNotBeNull();
        config.Credentials.GetUserName(config.GetMqttClientOptions()).Should().Be("user");
        config.Credentials.GetPassword(config.GetMqttClientOptions()).Should().BeEquivalentTo(passwordBytes);
    }

    [Fact]
    public void UseExtendedAuthenticationExchangeHandler_ShouldSetHandlerFromInstance()
    {
        TestExtendedAuthenticationExchangeHandler instance = new();
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.UseExtendedAuthenticationExchangeHandler(instance);

        MqttClientConfiguration config = builder.Build();
        config.ExtendedAuthenticationExchangeHandler.Should().BeSameAs(instance);
    }

    [Fact]
    public void UseExtendedAuthenticationExchangeHandler_ShouldSetHandlerFromGenericTypeArgument()
    {
        IServiceProvider? serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(TestExtendedAuthenticationExchangeHandler))
            .Returns(new TestExtendedAuthenticationExchangeHandler());

        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint(serviceProvider);

        builder.UseExtendedAuthenticationExchangeHandler<TestExtendedAuthenticationExchangeHandler>();

        MqttClientConfiguration config = builder.Build();
        config.ExtendedAuthenticationExchangeHandler.Should().BeOfType<TestExtendedAuthenticationExchangeHandler>();
    }

    [Fact]
    public void UseExtendedAuthenticationExchangeHandler_ShouldSetHandlerFromType()
    {
        IServiceProvider? serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(TestExtendedAuthenticationExchangeHandler))
            .Returns(new TestExtendedAuthenticationExchangeHandler());

        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint(serviceProvider);

        builder.UseExtendedAuthenticationExchangeHandler(typeof(TestExtendedAuthenticationExchangeHandler));

        MqttClientConfiguration config = builder.Build();
        config.ExtendedAuthenticationExchangeHandler.Should().BeOfType<TestExtendedAuthenticationExchangeHandler>();
    }

    // TODO: Test all ConnectTo() possibilities
    [Fact]
    public void ConnectTo_ShouldConnectViaTcp()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.ConnectTo("mqtt://test:42");

        MqttClientConfiguration configuration = builder.Build();

        configuration.Channel.As<MqttClientTcpConfiguration>().Server.Should().Be("test");
        configuration.Channel.As<MqttClientTcpConfiguration>().Port.Should().Be(42);
    }

    [Fact]
    public void ConnectTo_ShouldConnectViaTcpWithSsl()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.ConnectTo("mqtts://test:42");

        MqttClientConfiguration configuration = builder.Build();

        configuration.Channel.As<MqttClientTcpConfiguration>().Server.Should().Be("test");
        configuration.Channel.As<MqttClientTcpConfiguration>().Port.Should().Be(42);
        configuration.Channel.As<MqttClientTcpConfiguration>().Tls.UseTls.Should().BeTrue();
    }

    [Fact]
    public void ConnectViaTcp_ShouldSetChannelFromServerAndPort()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.ConnectViaTcp("tests-server", 1234);

        MqttClientConfiguration config = builder.Build();
        config.Channel.Should().BeOfType<MqttClientTcpConfiguration>();
        config.Channel.As<MqttClientTcpConfiguration>().Server.Should().Be("tests-server");
        config.Channel.As<MqttClientTcpConfiguration>().Port.Should().Be(1234);
    }

    [Fact]
    public void ConnectViaTcp_ShouldSetChannel()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

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
    public void ConnectViaWebSocket_ShouldSetChannelFromUri()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.ConnectViaWebSocket("uri");

        MqttClientConfiguration config = builder.Build();
        config.Channel.Should().BeOfType<MqttClientWebSocketConfiguration>();
        config.Channel.As<MqttClientWebSocketConfiguration>().Uri.Should().Be("uri");
    }

    [Fact]
    public void ConnectViaWebSocket_ShouldSetChannel()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

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
    public void UseProxy_ShouldSetProxyFromAddressEtc()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder
            .ConnectViaWebSocket("uri")
            .UseProxy(
                "address",
                "user",
                "pass",
                "domain",
                true,
                ["local1", "local2"]);

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
    public void UseProxy_ShouldSetProxy()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

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
    public void EnableTls_ShouldSetUseTls()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder
            .ConnectViaTcp("tests-server")
            .EnableTls();

        MqttClientConfiguration config = builder.Build();
        config.Channel.As<MqttClientTcpConfiguration>().Tls.UseTls.Should().BeTrue();
    }

    [Fact]
    public void DisableTls_ShouldSetUseTls()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder
            .ConnectViaTcp("tests-server")
            .DisableTls();

        MqttClientConfiguration config = builder.Build();
        config.Channel.As<MqttClientTcpConfiguration>().Tls.UseTls.Should().BeFalse();
    }

    [Fact]
    public void EnableTls_ShouldSetUseTlsSet_WhenConnectingViaWebSocket()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder
            .ConnectViaWebSocket("tests-server")
            .EnableTls(
                new MqttClientTlsConfiguration
                {
                    UseTls = true,
                    SslProtocol = SslProtocols.Tls12,
                    AllowUntrustedCertificates = true
                });

        MqttClientConfiguration config = builder.Build();
        config.Channel.As<MqttClientWebSocketConfiguration>().Tls.UseTls.Should().BeTrue();
        config.Channel.As<MqttClientWebSocketConfiguration>().Tls.SslProtocol.Should().Be(SslProtocols.Tls12);
        config.Channel.As<MqttClientWebSocketConfiguration>().Tls.AllowUntrustedCertificates.Should().BeTrue();
    }

    [Fact]
    public void DisableTls_ShouldSetUseTlsSet_WhenConnectingViaWebSocket()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder
            .ConnectViaWebSocket("tests-server")
            .DisableTls();

        MqttClientConfiguration config = builder.Build();
        config.Channel.As<MqttClientWebSocketConfiguration>().Tls.UseTls.Should().BeFalse();
    }

    [Fact]
    public void EnableTls_ShouldSetUseTls_WhenConnectingViaTcp()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

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
    public void DisableTls_ShouldSetUseTls_WhenConnectingViaTcp()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder
            .ConnectViaTcp("tests-server")
            .DisableTls();

        MqttClientConfiguration config = builder.Build();
        config.Channel.As<MqttClientTcpConfiguration>().Tls.UseTls.Should().BeFalse();
    }

    [Fact]
    public void EnableParallelProcessing_ShouldSetMaxDegreeOfParallelism()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.EnableParallelProcessing(42);

        MqttClientConfiguration configuration = builder.Build();
        configuration.MaxDegreeOfParallelism.Should().Be(42);
    }

    [Fact]
    public void DisableParallelProcessing_ShouldSetMaxDegreeOfParallelism()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint().EnableParallelProcessing(42);

        builder.DisableParallelProcessing();

        MqttClientConfiguration configuration = builder.Build();
        configuration.MaxDegreeOfParallelism.Should().Be(1);
    }

    [Fact]
    public void LimitBackpressure_ShouldSetBackpressureLimit()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.LimitBackpressure(42);

        MqttClientConfiguration configuration = builder.Build();
        configuration.BackpressureLimit.Should().Be(42);
    }

    private static MqttClientConfigurationBuilder GetBuilderWithValidConfigurationAndEndpoint(IServiceProvider? serviceProvider = null) =>
        GetBuilderWithValidConfiguration(serviceProvider).Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic"));

    private static MqttClientConfigurationBuilder GetBuilderWithValidConfiguration(IServiceProvider? serviceProvider = null) =>
        new MqttClientConfigurationBuilder(serviceProvider).ConnectViaTcp("test");

    private sealed class TestExtendedAuthenticationExchangeHandler : IMqttExtendedAuthenticationExchangeHandler
    {
        public Task HandleRequestAsync(MqttExtendedAuthenticationExchangeContext context) => Task.CompletedTask;
    }
}
