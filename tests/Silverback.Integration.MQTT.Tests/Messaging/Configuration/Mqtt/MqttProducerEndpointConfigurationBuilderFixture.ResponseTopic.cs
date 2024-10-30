// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Producing.Enrichers;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Configuration.Mqtt;

public partial class MqttProducerEndpointConfigurationBuilderFixture
{
    [Fact]
    public void SetResponseTopic_ShouldAddMessageEnricher()
    {
        MqttProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo("some-topic").SetResponseTopic("response-topic");

        MqttProducerEndpointConfiguration configuration = builder.Build();
        configuration.MessageEnrichers.Should().HaveCount(1);
        configuration.MessageEnrichers.Single().Should().BeOfType<ResponseTopicOutboundHeadersEnricher<TestEventOne>>();
    }

    [Fact]
    public void SetResponseTopic_ShouldAddMessageEnricherForChildType()
    {
        MqttProducerEndpointConfigurationBuilder<IIntegrationEvent> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo("some-topic").SetResponseTopic<TestEventOne>("response-topic");

        MqttProducerEndpointConfiguration configuration = builder.Build();
        configuration.MessageEnrichers.Should().HaveCount(1);
        configuration.MessageEnrichers.Single().Should().BeOfType<ResponseTopicOutboundHeadersEnricher<TestEventOne>>();
    }

    [Fact]
    public void SetResponseTopic_ShouldAddMessageEnricherWithValueFunction()
    {
        MqttProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo("some-topic").SetResponseTopic(message => message?.Content);

        MqttProducerEndpointConfiguration configuration = builder.Build();
        configuration.MessageEnrichers.Should().HaveCount(1);
        configuration.MessageEnrichers.Single().Should().BeOfType<ResponseTopicOutboundHeadersEnricher<TestEventOne>>();
    }

    [Fact]
    public void SetResponseTopic_ShouldAddMessageEnricherWithValueFunctionForChildType()
    {
        MqttProducerEndpointConfigurationBuilder<IIntegrationEvent> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo("some-topic").SetResponseTopic<TestEventOne>(message => message?.Content);

        MqttProducerEndpointConfiguration configuration = builder.Build();
        configuration.MessageEnrichers.Should().HaveCount(1);
        configuration.MessageEnrichers.Single().Should().BeOfType<ResponseTopicOutboundHeadersEnricher<TestEventOne>>();
    }

    [Fact]
    public void SetResponseTopic_ShouldAddMessageEnricherWithEnvelopeBasedValueFunction()
    {
        MqttProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo("some-topic").SetResponseTopic(envelope => envelope.GetMessageId());

        MqttProducerEndpointConfiguration configuration = builder.Build();
        configuration.MessageEnrichers.Should().HaveCount(1);
        configuration.MessageEnrichers.Single().Should().BeOfType<ResponseTopicOutboundHeadersEnricher<TestEventOne>>();
    }

    [Fact]
    public void SetResponseTopic_ShouldAddMessageEnricherWithEnvelopeBasedValueFunctionForChildType()
    {
        MqttProducerEndpointConfigurationBuilder<IIntegrationEvent> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo("some-topic").SetResponseTopic<TestEventOne>(envelope => envelope.GetMessageId());

        MqttProducerEndpointConfiguration configuration = builder.Build();
        configuration.MessageEnrichers.Should().HaveCount(1);
        configuration.MessageEnrichers.Single().Should().BeOfType<ResponseTopicOutboundHeadersEnricher<TestEventOne>>();
    }
}
