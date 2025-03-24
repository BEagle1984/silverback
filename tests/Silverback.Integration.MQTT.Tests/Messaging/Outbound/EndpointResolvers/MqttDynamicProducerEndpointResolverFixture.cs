// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using NSubstitute;
using Shouldly;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Outbound.EndpointResolvers;

public class MqttDynamicProducerEndpointResolverFixture
{
    private readonly IOutboundEnvelope<TestEventOne> _envelope = new OutboundEnvelope<TestEventOne>(
        new TestEventOne(),
        null,
        new MqttProducerEndpointConfiguration(),
        Substitute.For<IProducer>());

    [Fact]
    public void GetEndpoint_ShouldReturnTopicFromEnvelopeBasedTopicNameFunction()
    {
        MqttDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new((IOutboundEnvelope<TestEventOne> _) => "topic");

        ProducerEndpoint endpoint = endpointResolver.GetEndpoint(_envelope);

        MqttProducerEndpoint mqttEndpoint = endpoint.ShouldBeOfType<MqttProducerEndpoint>();
        mqttEndpoint.Topic.ShouldBe("topic");
    }

    [Fact]
    public void GetEndpoint_ShouldReturnTopicFromMessageBasedTopicNameFunction()
    {
        MqttDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new((TestEventOne? _) => "topic");

        ProducerEndpoint endpoint = endpointResolver.GetEndpoint(_envelope);

        MqttProducerEndpoint mqttEndpoint = endpoint.ShouldBeOfType<MqttProducerEndpoint>();
        mqttEndpoint.Topic.ShouldBe("topic");
    }

    [Fact]
    public void GetEndpoint_ShouldReturnTopicFromEnvelopeBasedTopicNameFormat()
    {
        MqttDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new(
            "topic-{0}",
            (IOutboundEnvelope<TestEventOne> _) => ["123"]);

        ProducerEndpoint endpoint = endpointResolver.GetEndpoint(_envelope);

        MqttProducerEndpoint mqttEndpoint = endpoint.ShouldBeOfType<MqttProducerEndpoint>();
        mqttEndpoint.Topic.ShouldBe("topic-123");
    }

    [Fact]
    public void GetEndpoint_ShouldReturnTopicFromMessageBasedTopicNameFormat()
    {
        MqttDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new(
            "topic-{0}",
            (TestEventOne? _) => ["123"]);

        ProducerEndpoint endpoint = endpointResolver.GetEndpoint(_envelope);

        MqttProducerEndpoint mqttEndpoint = endpoint.ShouldBeOfType<MqttProducerEndpoint>();
        mqttEndpoint.Topic.ShouldBe("topic-123");
    }

    [Fact]
    public void GetEndpoint_ShouldReturnTopicFromResolver()
    {
        MqttDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new(
            typeof(TestEndpointResolver),
            envelope => new TestEndpointResolver().GetTopic(envelope));

        IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(TestEndpointResolver)).Returns(new TestEndpointResolver());

        ProducerEndpoint endpoint = endpointResolver.GetEndpoint(_envelope);

        MqttProducerEndpoint mqttEndpoint = endpoint.ShouldBeOfType<MqttProducerEndpoint>();
        mqttEndpoint.Topic.ShouldBe("topic");
    }

    [Fact]
    public void RawName_ShouldReturnPlaceholderFromEnvelopeBasedTopicNameFunction()
    {
        MqttDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new((IOutboundEnvelope<TestEventOne> _) => "topic");

        endpointResolver.RawName.ShouldStartWith("dynamic-");
    }

    [Fact]
    public void RawName_ShouldReturnPlaceholderFromMessageBasedTopicNameFunction()
    {
        MqttDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new((TestEventOne? _) => "topic");

        endpointResolver.RawName.ShouldStartWith("dynamic-");
    }

    [Fact]
    public void RawName_ShouldReturnFormatStringFromEnvelopeBasedTopicFormat()
    {
        MqttDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new(
            "topic-{0}",
            (IOutboundEnvelope<TestEventOne> _) => ["123"]);

        endpointResolver.RawName.ShouldStartWith("topic-{0}");
    }

    [Fact]
    public void RawName_ShouldReturnFormatStringFromMessageBasedTopicFormat()
    {
        MqttDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new(
            "topic-{0}",
            (TestEventOne? _) => ["123"]);

        endpointResolver.RawName.ShouldStartWith("topic-{0}");
    }

    [Fact]
    public void RawName_ShouldReturnPlaceholderWithTypeNameFromResolver()
    {
        MqttDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new(
            typeof(TestEndpointResolver),
            envelope => new TestEndpointResolver().GetTopic(envelope));

        endpointResolver.RawName.ShouldStartWith("dynamic-TestEndpointResolver-");
    }

    [Fact]
    public void GetSerializedEndpoint_ShouldSerializeDestinationTopic()
    {
        MqttDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new((IOutboundEnvelope<TestEventOne> _) => "topic");

        string result = endpointResolver.GetSerializedEndpoint(_envelope);

        result.ShouldBe("topic");
    }

    [Fact]
    public void GetEndpoint_ShouldDeserializeEndpoint()
    {
        MqttDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new((IOutboundEnvelope<TestEventOne> _) => "topic");
        IOutboundEnvelope envelope = new OutboundEnvelope(
            null,
            [new MessageHeader(DefaultMessageHeaders.SerializedEndpoint, "serialized")],
            new MqttProducerEndpointConfiguration(),
            Substitute.For<IProducer>());

        ProducerEndpoint endpoint = endpointResolver.GetEndpoint(envelope);

        MqttProducerEndpoint mqttEndpoint = endpoint.ShouldBeOfType<MqttProducerEndpoint>();
        mqttEndpoint.Topic.ShouldBe("serialized");
    }

    private sealed class TestEndpointResolver : IMqttProducerEndpointResolver<TestEventOne>
    {
        public string GetTopic(IOutboundEnvelope<TestEventOne> envelope) => "topic";
    }
}
