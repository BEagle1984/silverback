// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MQTTnet.Protocol;
using NSubstitute;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public class MqttProducerEndpointConfigurationBuilderFixture
{
    [Fact]
    public void Build_ShouldThrow_WhenConfigurationIsNotValid()
    {
        MqttProducerEndpointConfigurationBuilder<TestEventOne> builder = new();

        Action act = () => builder.Build();

        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Fact]
    public void ProduceTo_ShouldSetEndpointFromTopicName()
    {
        MqttProducerEndpointConfiguration endpointConfiguration = new MqttProducerEndpointConfigurationBuilder<TestEventOne>()
            .ProduceTo("some-topic")
            .Build();

        endpointConfiguration.Endpoint.Should().BeOfType<MqttStaticProducerEndpointResolver>();
        endpointConfiguration.RawName.Should().Be("some-topic");
        MqttProducerEndpoint endpoint = (MqttProducerEndpoint)endpointConfiguration.Endpoint.GetEndpoint(
            null,
            endpointConfiguration,
            Substitute.For<IServiceProvider>());
        endpoint.Topic.Should().Be("some-topic");
    }

    [Fact]
    public void ProduceTo_ShouldSetEndpointFromTopicNameFunction()
    {
        MqttProducerEndpointConfiguration endpointConfiguration = new MqttProducerEndpointConfigurationBuilder<TestEventOne>()
            .ProduceTo(_ => "some-topic")
            .Build();

        endpointConfiguration.Endpoint.Should().BeOfType<MqttDynamicProducerEndpointResolver>();
        endpointConfiguration.RawName.Should().StartWith("dynamic-");
        MqttProducerEndpoint endpoint = (MqttProducerEndpoint)endpointConfiguration.Endpoint.GetEndpoint(
            null,
            endpointConfiguration,
            Substitute.For<IServiceProvider>());
        endpoint.Topic.Should().Be("some-topic");
    }

    [Fact]
    public void ProduceTo_ShouldSetEndpointFromTopicNameFormat()
    {
        MqttProducerEndpointConfiguration endpointConfiguration = new MqttProducerEndpointConfigurationBuilder<TestEventOne>()
            .ProduceTo("some-topic/{0}", _ => new[] { "123" })
            .Build();

        endpointConfiguration.Endpoint.Should().BeOfType<MqttDynamicProducerEndpointResolver>();
        endpointConfiguration.RawName.Should().Be("some-topic/{0}");
        MqttProducerEndpoint endpoint = (MqttProducerEndpoint)endpointConfiguration.Endpoint.GetEndpoint(
            null,
            endpointConfiguration,
            Substitute.For<IServiceProvider>());
        endpoint.Topic.Should().Be("some-topic/123");
    }

    [Fact]
    public void UseEndpointResolver_ShouldSetEndpoint()
    {
        MqttProducerEndpointConfiguration endpointConfiguration = new MqttProducerEndpointConfigurationBuilder<TestEventOne>()
            .UseEndpointResolver<TestEndpointResolver>()
            .Build();

        endpointConfiguration.Endpoint.Should().BeOfType<MqttDynamicProducerEndpointResolver>();
        endpointConfiguration.RawName.Should().StartWith("dynamic-TestEndpointResolver-");
    }

    [Fact]
    public void WithQualityOfServiceLevel_ShouldSetQualityOfServiceLevel()
    {
        MqttProducerEndpointConfigurationBuilder<TestEventOne> builder = new();
        builder
            .ProduceTo("some-topic")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce);
        MqttProducerEndpointConfiguration configuration = builder.Build();

        configuration.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.AtLeastOnce);
    }

    [Fact]
    public void WithAtMostOnceQoS_ShouldSetQualityOfServiceLevel()
    {
        MqttProducerEndpointConfigurationBuilder<TestEventOne> builder = new();
        builder
            .ProduceTo("some-topic")
            .WithAtMostOnceQoS();
        MqttProducerEndpointConfiguration configuration = builder.Build();

        configuration.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.AtMostOnce);
    }

    [Fact]
    public void WithAtLeastOnceQoS_ShouldSetQualityOfServiceLevel()
    {
        MqttProducerEndpointConfigurationBuilder<TestEventOne> builder = new();
        builder
            .ProduceTo("some-topic")
            .WithAtLeastOnceQoS();
        MqttProducerEndpointConfiguration configuration = builder.Build();

        configuration.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.AtLeastOnce);
    }

    [Fact]
    public void Retain_ShouldSetRetain()
    {
        MqttProducerEndpointConfigurationBuilder<TestEventOne> builder = new();
        builder
            .ProduceTo("some-topic")
            .Retain();
        MqttProducerEndpointConfiguration configuration = builder.Build();

        configuration.Retain.Should().BeTrue();
    }

    [Fact]
    public void WithMessageExpiration_ShouldSetMessageExpiryInterval()
    {
        MqttProducerEndpointConfigurationBuilder<TestEventOne> builder = new();
        builder
            .ProduceTo("some-topic")
            .WithMessageExpiration(TimeSpan.FromMinutes(42));
        MqttProducerEndpointConfiguration configuration = builder.Build();

        configuration.MessageExpiryInterval.Should().Be(42 * 60);
    }

    [SuppressMessage("", "CA1812", Justification = "Class used via DI")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Class used via DI")]
    private sealed class TestEndpointResolver : IMqttProducerEndpointResolver<TestEventOne>
    {
        public string GetTopic(TestEventOne? message) => "some-topic";
    }
}
