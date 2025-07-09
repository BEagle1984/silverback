// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public partial class MqttProducerEndpointConfigurationBuilderFixture
{
    private readonly IOutboundEnvelope<TestEventOne> _envelope = new OutboundEnvelope<TestEventOne>(
        new TestEventOne(),
        null,
        new MqttProducerEndpointConfiguration(),
        Substitute.For<IProducer>());

    [Fact]
    public void Build_ShouldThrow_WhenConfigurationIsNotValid()
    {
        MqttProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        Action act = () => builder.Build();

        act.ShouldThrow<BrokerConfigurationException>();
    }

    [Fact]
    public void ProduceTo_ShouldSetEndpointFromTopicName()
    {
        MqttProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo("some-topic");

        MqttProducerEndpointConfiguration endpointConfiguration = builder.Build();
        endpointConfiguration.EndpointResolver.ShouldBeOfType<MqttStaticProducerEndpointResolver>();
        endpointConfiguration.RawName.ShouldBe("some-topic");
        MqttProducerEndpoint configuration = (MqttProducerEndpoint)endpointConfiguration.EndpointResolver.GetEndpoint(_envelope);
        configuration.Topic.ShouldBe("some-topic");
    }

    [Fact]
    public void ProduceTo_ShouldSetEndpointFromTopicNameFunction()
    {
        MqttProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo(_ => "some-topic");

        MqttProducerEndpointConfiguration endpointConfiguration = builder.Build();
        endpointConfiguration.EndpointResolver.ShouldBeOfType<MqttDynamicProducerEndpointResolver<TestEventOne>>();
        endpointConfiguration.RawName.ShouldStartWith("dynamic-");
        MqttProducerEndpoint configuration = (MqttProducerEndpoint)endpointConfiguration.EndpointResolver.GetEndpoint(_envelope);
        configuration.Topic.ShouldBe("some-topic");
    }

    [Fact]
    public void ProduceTo_ShouldSetEndpointFromTopicNameFormat()
    {
        MqttProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo("some-topic/{0}", _ => ["123"]);

        MqttProducerEndpointConfiguration endpointConfiguration = builder.Build();
        endpointConfiguration.EndpointResolver.ShouldBeOfType<MqttDynamicProducerEndpointResolver<TestEventOne>>();
        endpointConfiguration.RawName.ShouldBe("some-topic/{0}");
        MqttProducerEndpoint configuration = (MqttProducerEndpoint)endpointConfiguration.EndpointResolver.GetEndpoint(_envelope);
        configuration.Topic.ShouldBe("some-topic/123");
    }

    [Fact]
    public void UseEndpointResolver_ShouldSetEndpoint()
    {
        MqttProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.UseEndpointResolver<TestEndpointResolver>();

        MqttProducerEndpointConfiguration endpointConfiguration = builder.Build();

        endpointConfiguration.EndpointResolver.ShouldBeOfType<MqttDynamicProducerEndpointResolver<TestEventOne>>();
        endpointConfiguration.RawName.ShouldStartWith("dynamic-TestEndpointResolver-");
    }

    [Fact]
    public void Retain_ShouldSetRetain()
    {
        MqttProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo("some-topic").Retain();

        MqttProducerEndpointConfiguration configuration = builder.Build();
        configuration.Retain.ShouldBeTrue();
    }

    [Fact]
    public void WithMessageExpiration_ShouldSetMessageExpiryInterval()
    {
        MqttProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo("some-topic").WithMessageExpiration(TimeSpan.FromMinutes(42));

        MqttProducerEndpointConfiguration configuration = builder.Build();
        configuration.MessageExpiryInterval.ShouldBe(42U * 60U);
    }

    [Fact]
    public void IgnoreNoMatchingSubscribers_ShouldSetNoMatchingSubscribersBehavior()
    {
        MqttProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo("some-topic").IgnoreNoMatchingSubscribers();

        MqttProducerEndpointConfiguration configuration = builder.Build();
        configuration.NoMatchingSubscribersBehavior.ShouldBe(NoMatchingSubscribersBehavior.Ignore);
    }

    [Fact]
    public void LogNoMatchingSubscribersWarning_ShouldSetNoMatchingSubscribersBehavior()
    {
        MqttProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo("some-topic").LogNoMatchingSubscribersWarning();

        MqttProducerEndpointConfiguration configuration = builder.Build();
        configuration.NoMatchingSubscribersBehavior.ShouldBe(NoMatchingSubscribersBehavior.LogWarning);
    }

    [Fact]
    public void ThrowNoMatchingSubscribersError_ShouldSetNoMatchingSubscribersBehavior()
    {
        MqttProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo("some-topic").ThrowNoMatchingSubscribersError();

        MqttProducerEndpointConfiguration configuration = builder.Build();
        configuration.NoMatchingSubscribersBehavior.ShouldBe(NoMatchingSubscribersBehavior.Throw);
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Class used via DI")]
    private sealed class TestEndpointResolver : IMqttProducerEndpointResolver<TestEventOne>
    {
        public string GetTopic(IOutboundEnvelope<TestEventOne> envelope) => "some-topic";
    }
}
