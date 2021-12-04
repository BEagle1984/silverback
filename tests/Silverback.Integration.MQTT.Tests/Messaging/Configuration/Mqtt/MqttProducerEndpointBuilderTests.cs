// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MQTTnet.Client.Options;
using MQTTnet.Protocol;
using NSubstitute;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Outbound.EndpointResolvers;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public class MqttProducerEndpointBuilderTests
{
    private readonly MqttClientConfiguration _clientConfiguration = new()
    {
        ChannelOptions = new MqttClientTcpOptions
        {
            Server = "tests-server"
        }
    };

    [Fact]
    public void Build_WithoutTopicName_ExceptionThrown()
    {
        MqttProducerConfigurationBuilder<TestEventOne> builder = new(_clientConfiguration);

        Action act = () => builder.Build();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void Build_WithoutServer_ExceptionThrown()
    {
        MqttProducerConfigurationBuilder<TestEventOne> builder = new(new MqttClientConfiguration());
        builder.ProduceTo("some-topic");

        Action act = () => builder.Build();

        act.Should().ThrowExactly<EndpointConfigurationException>();
    }

    [Fact]
    public void ProduceTo_TopicName_EndpointSet()
    {
        MqttProducerConfiguration endpointConfiguration = new MqttProducerConfigurationBuilder<TestEventOne>(_clientConfiguration)
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
    public void ProduceTo_TopicNameFunction_EndpointSet()
    {
        MqttProducerConfiguration endpointConfiguration = new MqttProducerConfigurationBuilder<TestEventOne>(_clientConfiguration)
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
    public void ProduceTo_TopicNameFormat_EndpointSet()
    {
        MqttProducerConfiguration endpointConfiguration = new MqttProducerConfigurationBuilder<TestEventOne>(_clientConfiguration)
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
    public void UseEndpointResolver_EndpointSet()
    {
        MqttProducerConfiguration endpointConfiguration = new MqttProducerConfigurationBuilder<TestEventOne>(_clientConfiguration)
            .UseEndpointResolver<TestEndpointResolver>()
            .Build();

        endpointConfiguration.Endpoint.Should().BeOfType<MqttDynamicProducerEndpointResolver>();
        endpointConfiguration.RawName.Should().StartWith("dynamic-TestEndpointResolver-");
    }

    [Fact]
    public void Configure_BuilderAction_ConfigurationMergedWithBase()
    {
        MqttProducerConfigurationBuilder<TestEventOne> builder = new(_clientConfiguration);
        builder
            .ProduceTo("some-topic")
            .ConfigureClient(configuration => configuration.WithClientId("client42"));
        MqttProducerConfiguration configuration = builder.Build();

        configuration.Client.ChannelOptions.Should().NotBeNull();
        configuration.Client.ChannelOptions.Should().BeEquivalentTo(_clientConfiguration.ChannelOptions);
        configuration.Client.ClientId.Should().Be("client42");
    }

    [Fact]
    public void WithQualityOfServiceLevel_QualityOfServiceLevelSet()
    {
        MqttProducerConfigurationBuilder<TestEventOne> builder = new(_clientConfiguration);
        builder
            .ProduceTo("some-topic")
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce);
        MqttProducerConfiguration configuration = builder.Build();

        configuration.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.AtLeastOnce);
    }

    [Fact]
    public void WithAtMostOnceQoS_QualityOfServiceLevelSet()
    {
        MqttProducerConfigurationBuilder<TestEventOne> builder = new(_clientConfiguration);
        builder
            .ProduceTo("some-topic")
            .WithAtMostOnceQoS();
        MqttProducerConfiguration configuration = builder.Build();

        configuration.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.AtMostOnce);
    }

    [Fact]
    public void WithAtLeastOnceQoS_QualityOfServiceLevelSet()
    {
        MqttProducerConfigurationBuilder<TestEventOne> builder = new(_clientConfiguration);
        builder
            .ProduceTo("some-topic")
            .WithAtLeastOnceQoS();
        MqttProducerConfiguration configuration = builder.Build();

        configuration.QualityOfServiceLevel.Should().Be(MqttQualityOfServiceLevel.AtLeastOnce);
    }

    [Fact]
    public void Retain_RetainSet()
    {
        MqttProducerConfigurationBuilder<TestEventOne> builder = new(_clientConfiguration);
        builder
            .ProduceTo("some-topic")
            .Retain();
        MqttProducerConfiguration configuration = builder.Build();

        configuration.Retain.Should().BeTrue();
    }

    [Fact]
    public void WithMessageExpiration_MessageExpiryIntervalSet()
    {
        MqttProducerConfigurationBuilder<TestEventOne> builder = new(_clientConfiguration);
        builder
            .ProduceTo("some-topic")
            .WithMessageExpiration(TimeSpan.FromMinutes(42));
        MqttProducerConfiguration configuration = builder.Build();

        configuration.MessageExpiryInterval.Should().Be(42 * 60);
    }

    [SuppressMessage("", "CA1812", Justification = "Class used via DI")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Class used via DI")]
    private sealed class TestEndpointResolver : IMqttProducerEndpointResolver<TestEventOne>
    {
        public string GetTopic(TestEventOne? message) => "some-topic";
    }
}
