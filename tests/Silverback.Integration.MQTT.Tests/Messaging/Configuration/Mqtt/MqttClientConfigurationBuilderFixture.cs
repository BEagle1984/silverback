// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using NSubstitute;
using Shouldly;
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
        MqttClientConfigurationBuilder builder = new(Substitute.For<IServiceProvider>());

        builder
            .ConnectViaTcp("tests-server")
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic"));

        MqttClientConfiguration configuration = builder.Build();
        configuration.ProtocolVersion.ShouldBe(MqttProtocolVersion.V500);
    }

    [Fact]
    public void Build_ShouldThrow_WhenConfigurationIsNotValid()
    {
        MqttClientConfigurationBuilder builder = new(Substitute.For<IServiceProvider>());

        Action act = () => builder.Build();

        act.ShouldThrow<SilverbackConfigurationException>();
    }

    [Fact]
    public void Build_ShouldReturnNewConfiguration_WhenReused()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.WithClientId("one");
        MqttClientConfiguration configuration1 = builder.Build();

        builder.WithClientId("two");
        MqttClientConfiguration configuration2 = builder.Build();

        configuration1.ClientId.ShouldBe("one");
        configuration2.ClientId.ShouldBe("two");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Build_ShouldInferPortFromTls(bool useTls)
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder
            .ConnectViaTcp("tests-server")
            .EnableTls(new MqttClientTlsConfiguration { UseTls = useTls });

        MqttClientConfiguration configuration = builder.Build();
        MqttClientTcpConfiguration tcpConfiguration = configuration.Channel.ShouldBeOfType<MqttClientTcpConfiguration>();
        tcpConfiguration.RemoteEndpoint.ShouldBe(new DnsEndPoint("tests-server", useTls ? 8883 : 1883));
    }

    [Fact]
    public void Produce_ShouldAddEndpoints()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1"))
            .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo("topic2"));

        MqttClientConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.ProducerEndpoints.Count.ShouldBe(2);
        MqttProducerEndpointConfiguration endpoint1 = configuration.ProducerEndpoints.First();
        MqttStaticProducerEndpointResolver resolver1 = endpoint1.EndpointResolver.ShouldBeOfType<MqttStaticProducerEndpointResolver>();
        resolver1.Topic.ShouldBe("topic1");
        endpoint1.Serializer.ShouldBeOfType<JsonMessageSerializer>();
        MqttProducerEndpointConfiguration endpoint2 = configuration.ProducerEndpoints.Skip(1).First();
        MqttStaticProducerEndpointResolver resolver2 = endpoint2.EndpointResolver.ShouldBeOfType<MqttStaticProducerEndpointResolver>();
        resolver2.Topic.ShouldBe("topic2");
        endpoint2.Serializer.ShouldBeOfType<JsonMessageSerializer>();
    }

    [Fact]
    public void Produce_ShouldAddEndpointWithGenericMessageType_WhenNoTypeIsSpecified()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder.Produce(endpoint => endpoint.ProduceTo("topic1"));

        MqttClientConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.ProducerEndpoints.Count.ShouldBe(1);
        MqttProducerEndpointConfiguration endpoint = configuration.ProducerEndpoints.Single();
        endpoint.MessageType.ShouldBe(typeof(object));
        MqttStaticProducerEndpointResolver resolver = endpoint.EndpointResolver.ShouldBeOfType<MqttStaticProducerEndpointResolver>();
        resolver.Topic.ShouldBe("topic1");
        endpoint.Serializer.ShouldBeOfType<JsonMessageSerializer>();
    }

    [Fact]
    public void Produce_ShouldIgnoreEndpointsWithSameId()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Produce<TestEventOne>("id1", endpoint => endpoint.ProduceTo("topic1"))
            .Produce<TestEventTwo>("id1", endpoint => endpoint.ProduceTo("topic1"));

        MqttClientConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.ProducerEndpoints.Count.ShouldBe(1);
        MqttProducerEndpointConfiguration endpoint = configuration.ProducerEndpoints.First();
        MqttStaticProducerEndpointResolver resolver = endpoint.EndpointResolver.ShouldBeOfType<MqttStaticProducerEndpointResolver>();
        resolver.Topic.ShouldBe("topic1");
        endpoint.Serializer.ShouldBeOfType<JsonMessageSerializer>();
    }

    [Fact]
    public void Produce_ShouldIgnoreEndpointsWithSameTopicNameAndMessageTypeAndNoId()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1").WithExactlyOnceQoS())
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1").WithAtLeastOnceQoS());

        MqttClientConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.ProducerEndpoints.Count.ShouldBe(1);
        MqttProducerEndpointConfiguration endpoint = configuration.ProducerEndpoints.First();
        MqttStaticProducerEndpointResolver resolver = endpoint.EndpointResolver.ShouldBeOfType<MqttStaticProducerEndpointResolver>();
        resolver.Topic.ShouldBe("topic1");
        endpoint.Serializer.ShouldBeOfType<JsonMessageSerializer>();
        endpoint.QualityOfServiceLevel.ShouldBe(MqttQualityOfServiceLevel.ExactlyOnce);
    }

    [Fact]
    public void Produce_ShouldAddEndpointsWithSameTopicNameAndDifferentMessageType()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1"))
            .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo("topic1"));

        MqttClientConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.ProducerEndpoints.Count.ShouldBe(2);
    }

    [Fact]
    public void Produce_ShouldAddEndpointsWithSameTopicNameAndDifferentId()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Produce<TestEventOne>("id1", endpoint => endpoint.ProduceTo("topic1"))
            .Produce<TestEventTwo>("id2", endpoint => endpoint.ProduceTo("topic1"));

        MqttClientConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.ProducerEndpoints.Count.ShouldBe(2);
    }

    [Fact]
    public void Consume_ShouldAddEndpointsWithoutMessageType()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Consume(endpoint => endpoint.ConsumeFrom("topic1"))
            .Consume(endpoint => endpoint.ConsumeFrom("topic2"));

        MqttClientConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.ConsumerEndpoints.Count.ShouldBe(2);
        MqttConsumerEndpointConfiguration endpoint1 = configuration.ConsumerEndpoints.First();
        endpoint1.Topics.Count.ShouldBe(1);
        endpoint1.Topics.First().ShouldBe("topic1");
        endpoint1.Deserializer.ShouldBeOfType<JsonMessageDeserializer<object>>();
        MqttConsumerEndpointConfiguration endpoint2 = configuration.ConsumerEndpoints.Skip(1).First();
        endpoint2.Topics.Count.ShouldBe(1);
        endpoint2.Topics.First().ShouldBe("topic2");
        endpoint2.Deserializer.ShouldBeOfType<JsonMessageDeserializer<object>>();
    }

    [Fact]
    public void Consume_ShouldAddEndpointsForMessageType()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom("topic1"))
            .Consume<TestEventTwo>(endpoint => endpoint.ConsumeFrom("topic2"));

        MqttClientConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.ConsumerEndpoints.Count.ShouldBe(2);
        MqttConsumerEndpointConfiguration endpoint1 = configuration.ConsumerEndpoints.First();
        endpoint1.Topics.Count.ShouldBe(1);
        endpoint1.Topics.First().ShouldBe("topic1");
        endpoint1.Deserializer.ShouldBeOfType<JsonMessageDeserializer<TestEventOne>>();
        MqttConsumerEndpointConfiguration endpoint2 = configuration.ConsumerEndpoints.Skip(1).First();
        endpoint2.Topics.Count.ShouldBe(1);
        endpoint2.Topics.First().ShouldBe("topic2");
        endpoint2.Deserializer.ShouldBeOfType<JsonMessageDeserializer<TestEventTwo>>();
    }

    [Fact]
    public void Consume_ShouldIgnoreEndpointsWithSameId()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Consume("id1", endpoint => endpoint.ConsumeFrom("topic1"))
            .Consume<TestEventTwo>("id1", endpoint => endpoint.ConsumeFrom("topic2"));

        MqttClientConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.ConsumerEndpoints.Count.ShouldBe(1);
        configuration.ConsumerEndpoints.First().Topics.Count.ShouldBe(1);
        configuration.ConsumerEndpoints.First().Topics.ShouldBe(["topic1"]);
        configuration.ConsumerEndpoints.First().Deserializer.ShouldBeOfType<JsonMessageDeserializer<object>>();
    }

    [Fact]
    public void Consume_ShouldIgnoreEndpointsWithSameTopicNameAndMessageTypeAndNoId()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom("topic1").WithExactlyOnceQoS())
            .Consume<TestEventTwo>(endpoint => endpoint.ConsumeFrom("topic1").WithAtLeastOnceQoS());

        MqttClientConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.ConsumerEndpoints.Count.ShouldBe(1);
        MqttConsumerEndpointConfiguration endpoint1 = configuration.ConsumerEndpoints.First();
        endpoint1.Topics.Count.ShouldBe(1);
        endpoint1.Topics.First().ShouldBe("topic1");
        endpoint1.Deserializer.ShouldBeOfType<JsonMessageDeserializer<TestEventOne>>();
        endpoint1.QualityOfServiceLevel.ShouldBe(MqttQualityOfServiceLevel.ExactlyOnce);
    }

    [Fact]
    public void Consume_ShouldAddEndpointsWithSameTopicNameAndDifferentId()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Consume("id1", endpoint => endpoint.ConsumeFrom("topic1"))
            .Consume<TestEventTwo>("id2", endpoint => endpoint.ConsumeFrom("topic1"));

        Action act = () => builder.Build();

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("Cannot connect to the same topic in different endpoints in the same consumer.");
    }

    [Fact]
    public void UseProtocolVersion_ShouldSetProtocolVersion()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.UseProtocolVersion(MqttProtocolVersion.V311);

        MqttClientConfiguration configuration = builder.Build();
        configuration.ProtocolVersion.ShouldBe(MqttProtocolVersion.V311);
    }

    [Fact]
    public void WithTimeout_ShouldSetTimeout()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.WithTimeout(TimeSpan.FromSeconds(42));

        MqttClientConfiguration configuration = builder.Build();
        configuration.Timeout.TotalSeconds.ShouldBe(42);
    }

    [Fact]
    public void RequestCleanSession_ShouldSetCleanSession()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.RequestCleanSession();

        MqttClientConfiguration configuration = builder.Build();
        configuration.CleanSession.ShouldBeTrue();
    }

    [Fact]
    public void RequestPersistentSession_ShouldSetCleanSession()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.RequestPersistentSession();

        MqttClientConfiguration configuration = builder.Build();
        configuration.CleanSession.ShouldBeFalse();
    }

    [Fact]
    public void DisableKeepAlive_ShouldSetKeepAliveInterval()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.DisableKeepAlive();

        MqttClientConfiguration configuration = builder.Build();
        configuration.KeepAlivePeriod.ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void SendKeepAlive_ShouldSetKeepAliveInterval()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.SendKeepAlive(TimeSpan.FromMinutes(42));

        MqttClientConfiguration configuration = builder.Build();
        configuration.KeepAlivePeriod.ShouldBe(TimeSpan.FromMinutes(42));
    }

    [Fact]
    public void WithClientId_ShouldSetClientId()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.WithClientId("client-42");

        MqttClientConfiguration configuration = builder.Build();
        configuration.ClientId.ShouldBe("client-42");
    }

    [Fact]
    public void SendLastWillMessage_ShouldSetWillMessageAndDelay()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.SendLastWillMessage<TestEventOne>(
            lastWill => lastWill
                .SendMessage(
                    new TestEventOne
                    {
                        Content = "I died!"
                    })
                .WithDelay(TimeSpan.FromSeconds(42))
                .ProduceTo("testaments"));

        MqttClientConfiguration configuration = builder.Build();
        configuration.WillMessage.ShouldNotBeNull();
        configuration.WillMessage.Topic.ShouldBe("testaments");
        configuration.WillMessage.Payload.ShouldNotBeEmpty();
        configuration.WillMessage.Delay.ShouldBe(42U);
    }

    [Fact]
    public void WithEnhancedAuthentication_ShouldSetAuthenticationMethodAndData()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.WithEnhancedAuthentication("method", [0x01, 0x02, 0x03]);

        MqttClientConfiguration configuration = builder.Build();
        configuration.AuthenticationMethod.ShouldBe("method");
        configuration.AuthenticationData.ShouldBe([0x01, 0x02, 0x03]);
    }

    [Fact]
    public void LimitTopicAlias_ShouldSetTopicAliasMaximum()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.LimitTopicAlias(42);

        MqttClientConfiguration configuration = builder.Build();
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
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        Action act = () => builder.LimitTopicAlias(value);

        if (isValid)
            act.ShouldNotThrow();
        else
            act.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void LimitPacketSize_ShouldSetMaximumPacketSize()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.LimitPacketSize(42);

        MqttClientConfiguration configuration = builder.Build();
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
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        Action act = () => builder.LimitPacketSize(value);

        if (isValid)
            act.ShouldNotThrow();
        else
            act.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void LimitUnacknowledgedPublications_ShouldSetReceiveMaximum()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.LimitUnacknowledgedPublications(42);

        MqttClientConfiguration configuration = builder.Build();
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
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        Action act = () => builder.LimitUnacknowledgedPublications(value);

        if (isValid)
            act.ShouldNotThrow();
        else
            act.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RequestProblemInformation_ShouldSetRequestProblemInformation()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.RequestProblemInformation();

        MqttClientConfiguration configuration = builder.Build();
        configuration.RequestProblemInformation.ShouldBeTrue();
    }

    [Fact]
    public void DisableProblemInformation_ShouldSetRequestProblemInformation()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.DisableProblemInformation();

        MqttClientConfiguration configuration = builder.Build();
        configuration.RequestProblemInformation.ShouldBeFalse();
    }

    [Fact]
    public void RequestResponseInformation_ShouldSetRequestResponseInformation()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.RequestResponseInformation();

        MqttClientConfiguration configuration = builder.Build();
        configuration.RequestResponseInformation.ShouldBeTrue();
    }

    [Fact]
    public void DisableResponseInformation_ShouldSetRequestResponseInformation()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.DisableResponseInformation();

        MqttClientConfiguration configuration = builder.Build();
        configuration.RequestResponseInformation.ShouldBeFalse();
    }

    [Fact]
    public void WithSessionExpiration_ShouldSetSessionExpiryInterval()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.WithSessionExpiration(TimeSpan.FromSeconds(42));

        MqttClientConfiguration configuration = builder.Build();
        configuration.SessionExpiryInterval.ShouldBe(42U);
    }

    [Fact]
    public void AddUserProperty_ShouldAddUserProperties()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder
            .AddUserProperty("prop1", "value1")
            .AddUserProperty("prop2", "value2");

        MqttClientConfiguration configuration = builder.Build();
        configuration.UserProperties.ShouldBe(
        [
            new MqttUserProperty("prop1", "value1"),
            new MqttUserProperty("prop2", "value2")
        ]);
    }

    [Fact]
    public void WithCredentials_ShouldSetCredentialsWithStringPassword()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.WithCredentials("user", "pass");

        MqttClientConfiguration configuration = builder.Build();
        configuration.Credentials.ShouldNotBeNull();
        configuration.Credentials.GetUserName(configuration.GetMqttClientOptions()).ShouldBe("user");
        configuration.Credentials.GetPassword(configuration.GetMqttClientOptions()).ShouldBe("pass"u8.ToArray());
    }

    [Fact]
    public void WithCredentials_ShouldSetCredentialsWithBytesPassword()
    {
        byte[] passwordBytes = "pass"u8.ToArray();
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.WithCredentials("user", passwordBytes);

        MqttClientConfiguration configuration = builder.Build();
        configuration.Credentials.ShouldNotBeNull();
        configuration.Credentials.GetUserName(configuration.GetMqttClientOptions()).ShouldBe("user");
        configuration.Credentials.GetPassword(configuration.GetMqttClientOptions()).ShouldBe(passwordBytes);
    }

    [Fact]
    public void WithCredentials_ShouldSetCredentialsFromClientCredentials()
    {
        byte[] passwordBytes = "pass"u8.ToArray();
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.WithCredentials(new MqttClientCredentials("user", passwordBytes));

        MqttClientConfiguration configuration = builder.Build();
        configuration.Credentials.ShouldNotBeNull();
        configuration.Credentials.GetUserName(configuration.GetMqttClientOptions()).ShouldBe("user");
        configuration.Credentials.GetPassword(configuration.GetMqttClientOptions()).ShouldBe(passwordBytes);
    }

    [Fact]
    public void UseEnhancedAuthenticationExchangeHandler_ShouldSetHandlerFromInstance()
    {
        TestEnhancedAuthenticationHandler instance = new();
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.UseEnhancedAuthenticationHandler(instance);

        MqttClientConfiguration configuration = builder.Build();
        configuration.EnhancedAuthenticationHandler.ShouldBeSameAs(instance);
    }

    [Fact]
    public void UseEnhancedAuthenticationHandler_ShouldSetHandlerFromGenericTypeArgument()
    {
        IServiceProvider? serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(TestEnhancedAuthenticationHandler))
            .Returns(new TestEnhancedAuthenticationHandler());

        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint(serviceProvider);

        builder.UseEnhancedAuthenticationHandler<TestEnhancedAuthenticationHandler>();

        MqttClientConfiguration configuration = builder.Build();
        configuration.EnhancedAuthenticationHandler.ShouldBeOfType<TestEnhancedAuthenticationHandler>();
    }

    [Fact]
    public void UseEnhancedAuthenticationHandler_ShouldSetHandlerFromType()
    {
        IServiceProvider? serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(TestEnhancedAuthenticationHandler))
            .Returns(new TestEnhancedAuthenticationHandler());

        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint(serviceProvider);

        builder.UseEnhancedAuthenticationHandler(typeof(TestEnhancedAuthenticationHandler));

        MqttClientConfiguration configuration = builder.Build();
        configuration.EnhancedAuthenticationHandler.ShouldBeOfType<TestEnhancedAuthenticationHandler>();
    }

    [Theory]
    [InlineData("mqtt://test:42")]
    [InlineData("tcp://test:42")]
    public void ConnectTo_ShouldConnectViaTcp(string uri)
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.ConnectTo(uri);

        MqttClientConfiguration configuration = builder.Build();

        MqttClientTcpConfiguration tcpConfiguration = configuration.Channel.ShouldBeOfType<MqttClientTcpConfiguration>();
        tcpConfiguration.RemoteEndpoint.ShouldBe(new DnsEndPoint("test", 42));
        tcpConfiguration.Tls.UseTls.ShouldBeFalse();
    }

    [Fact]
    public void ConnectTo_ShouldConnectViaTcpWithSsl()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.ConnectTo("mqtts://test:42");

        MqttClientConfiguration configuration = builder.Build();

        MqttClientTcpConfiguration tcpConfiguration = configuration.Channel.ShouldBeOfType<MqttClientTcpConfiguration>();
        tcpConfiguration.RemoteEndpoint.ShouldBe(new DnsEndPoint("test", 42));
        tcpConfiguration.Tls.UseTls.ShouldBeTrue();
    }

    [Theory]
    [InlineData("ws://test:42/")]
    [InlineData("wss://test:42/")]
    public void ConnectTo_ShouldConnectViaWebSocket(string uri)
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.ConnectTo(uri);

        MqttClientConfiguration configuration = builder.Build();

        MqttClientWebSocketConfiguration webSocketConfiguration = configuration.Channel.ShouldBeOfType<MqttClientWebSocketConfiguration>();
        webSocketConfiguration.Uri.ShouldBe(uri);
        webSocketConfiguration.Tls.UseTls.ShouldBeFalse();
    }

    [Fact]
    public void ConnectViaTcp_ShouldSetChannelAndRemoteEndpointFromServer()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.ConnectViaTcp("tests-server");

        MqttClientConfiguration configuration = builder.Build();
        MqttClientTcpConfiguration tcpConfiguration = configuration.Channel.ShouldBeOfType<MqttClientTcpConfiguration>();
        tcpConfiguration.RemoteEndpoint.ShouldBe(new DnsEndPoint("tests-server", 1883));
    }

    [Fact]
    public void ConnectViaTcp_ShouldSetChannelAndRemoteEndpointFromServerAndPort()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.ConnectViaTcp("tests-server", 1234, AddressFamily.InterNetworkV6, ProtocolType.IP);

        MqttClientConfiguration configuration = builder.Build();
        MqttClientTcpConfiguration tcpConfiguration = configuration.Channel.ShouldBeOfType<MqttClientTcpConfiguration>();
        tcpConfiguration.RemoteEndpoint.ShouldBe(
            new DnsEndPoint(
                "tests-server",
                1234,
                AddressFamily.InterNetworkV6));
        tcpConfiguration.ProtocolType.ShouldBe(ProtocolType.IP);
    }

    [Fact]
    public void ConnectViaTcp_ShouldSetChannelAndRemoteEndpointFromServerAndPortAndAddressFamilyAndProtocolType()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.ConnectViaTcp("tests-server", 1234);

        MqttClientConfiguration configuration = builder.Build();
        MqttClientTcpConfiguration tcpConfiguration = configuration.Channel.ShouldBeOfType<MqttClientTcpConfiguration>();
        tcpConfiguration.RemoteEndpoint.ShouldBe(new DnsEndPoint("tests-server", 1234));
    }

    [Fact]
    public void ConnectViaTcp_ShouldSetChannel()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.ConnectViaTcp(
            new MqttClientTcpConfiguration
            {
                RemoteEndpoint = new DnsEndPoint("tests-server", 1234),
            });

        MqttClientConfiguration configuration = builder.Build();
        MqttClientTcpConfiguration tcpConfiguration = configuration.Channel.ShouldBeOfType<MqttClientTcpConfiguration>();
        tcpConfiguration.RemoteEndpoint.ShouldBe(new DnsEndPoint("tests-server", 1234));
    }

    [Fact]
    public void ConnectViaWebSocket_ShouldSetChannelFromUri()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.ConnectViaWebSocket("uri");

        MqttClientConfiguration configuration = builder.Build();
        MqttClientWebSocketConfiguration webSocketConfiguration = configuration.Channel.ShouldBeOfType<MqttClientWebSocketConfiguration>();
        webSocketConfiguration.Uri.ShouldBe("uri");
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

        MqttClientConfiguration configuration = builder.Build();
        MqttClientWebSocketConfiguration webSocketConfiguration = configuration.Channel.ShouldBeOfType<MqttClientWebSocketConfiguration>();
        webSocketConfiguration.Uri.ShouldBe("uri");
        webSocketConfiguration.RequestHeaders.ShouldBe(
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

        MqttClientConfiguration configuration = builder.Build();
        MqttClientWebSocketConfiguration webSocketConfiguration = configuration.Channel.ShouldBeOfType<MqttClientWebSocketConfiguration>();
        webSocketConfiguration.Proxy.ShouldNotBeNull();
        webSocketConfiguration.Proxy!.Address.ShouldBe("address");
        webSocketConfiguration.Proxy!.Username.ShouldBe("user");
        webSocketConfiguration.Proxy!.Password.ShouldBe("pass");
        webSocketConfiguration.Proxy!.Domain.ShouldBe("domain");
        webSocketConfiguration.Proxy!.BypassOnLocal.ShouldBeTrue();
        webSocketConfiguration.Proxy!.BypassList.ShouldBe(["local1", "local2"]);
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

        MqttClientConfiguration configuration = builder.Build();
        MqttClientWebSocketConfiguration webSocketConfiguration = configuration.Channel.ShouldBeOfType<MqttClientWebSocketConfiguration>();
        webSocketConfiguration.Proxy.ShouldNotBeNull();
        webSocketConfiguration.Proxy!.Address.ShouldBe("address");
        webSocketConfiguration.Proxy!.Username.ShouldBe("user");
        webSocketConfiguration.Proxy!.Password.ShouldBe("pass");
        webSocketConfiguration.Proxy!.Domain.ShouldBe("domain");
        webSocketConfiguration.Proxy!.BypassOnLocal.ShouldBeTrue();
        webSocketConfiguration.Proxy!.BypassList.ShouldBe(["local1", "local2"]);
    }

    [Fact]
    public void EnableTls_ShouldSetUseTls()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder
            .ConnectViaTcp("tests-server")
            .EnableTls();

        MqttClientConfiguration configuration = builder.Build();
        MqttClientTcpConfiguration tcpConfiguration = configuration.Channel.ShouldBeOfType<MqttClientTcpConfiguration>();
        tcpConfiguration.Tls.UseTls.ShouldBeTrue();
    }

    [Fact]
    public void DisableTls_ShouldSetUseTls()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder
            .ConnectViaTcp("tests-server")
            .DisableTls();

        MqttClientConfiguration configuration = builder.Build();
        MqttClientTcpConfiguration tcpConfiguration = configuration.Channel.ShouldBeOfType<MqttClientTcpConfiguration>();
        tcpConfiguration.Tls.UseTls.ShouldBeFalse();
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

        MqttClientConfiguration configuration = builder.Build();
        MqttClientWebSocketConfiguration webSocketConfiguration = configuration.Channel.ShouldBeOfType<MqttClientWebSocketConfiguration>();
        webSocketConfiguration.Tls.UseTls.ShouldBeTrue();
        webSocketConfiguration.Tls.SslProtocol.ShouldBe(SslProtocols.Tls12);
        webSocketConfiguration.Tls.AllowUntrustedCertificates.ShouldBeTrue();
    }

    [Fact]
    public void DisableTls_ShouldSetUseTlsSet_WhenConnectingViaWebSocket()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder
            .ConnectViaWebSocket("tests-server")
            .DisableTls();

        MqttClientConfiguration configuration = builder.Build();
        MqttClientWebSocketConfiguration webSocketConfiguration = configuration.Channel.ShouldBeOfType<MqttClientWebSocketConfiguration>();
        webSocketConfiguration.Tls.UseTls.ShouldBeFalse();
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

        MqttClientConfiguration configuration = builder.Build();
        MqttClientTcpConfiguration tcpConfiguration = configuration.Channel.ShouldBeOfType<MqttClientTcpConfiguration>();
        tcpConfiguration.Tls.UseTls.ShouldBeTrue();
        tcpConfiguration.Tls.SslProtocol.ShouldBe(SslProtocols.Tls12);
        tcpConfiguration.Tls.AllowUntrustedCertificates.ShouldBeTrue();
    }

    [Fact]
    public void DisableTls_ShouldSetUseTls_WhenConnectingViaTcp()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder
            .ConnectViaTcp("tests-server")
            .DisableTls();

        MqttClientConfiguration configuration = builder.Build();
        MqttClientTcpConfiguration tcpConfiguration = configuration.Channel.ShouldBeOfType<MqttClientTcpConfiguration>();
        tcpConfiguration.Tls.UseTls.ShouldBeFalse();
    }

    [Fact]
    public void EnableParallelProcessing_ShouldSetMaxDegreeOfParallelism()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.EnableParallelProcessing(42);

        MqttClientConfiguration configuration = builder.Build();
        configuration.MaxDegreeOfParallelism.ShouldBe(42);
    }

    [Fact]
    public void DisableParallelProcessing_ShouldSetMaxDegreeOfParallelism()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint().EnableParallelProcessing(42);

        builder.DisableParallelProcessing();

        MqttClientConfiguration configuration = builder.Build();
        configuration.MaxDegreeOfParallelism.ShouldBe(1);
    }

    [Fact]
    public void LimitBackpressure_ShouldSetBackpressureLimit()
    {
        MqttClientConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.LimitBackpressure(42);

        MqttClientConfiguration configuration = builder.Build();
        configuration.BackpressureLimit.ShouldBe(42);
    }

    private static MqttClientConfigurationBuilder GetBuilderWithValidConfigurationAndEndpoint(IServiceProvider? serviceProvider = null) =>
        GetBuilderWithValidConfiguration(serviceProvider)
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic"));

    private static MqttClientConfigurationBuilder GetBuilderWithValidConfiguration(IServiceProvider? serviceProvider = null) =>
        new MqttClientConfigurationBuilder(serviceProvider ?? Substitute.For<IServiceProvider>())
            .ConnectViaTcp("test");

    private sealed class TestEnhancedAuthenticationHandler : IMqttEnhancedAuthenticationHandler
    {
        public Task HandleEnhancedAuthenticationAsync(MqttEnhancedAuthenticationEventArgs eventArgs) => Task.CompletedTask;
    }
}
