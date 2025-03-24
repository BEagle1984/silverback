// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Messages;

public class MqttEnvelopeExtensionsFixture
{
    [Fact]
    public void GetMqttResponseTopic_ShouldReturnResponseTopic()
    {
        InboundEnvelope envelope = new(
            null,
            [new MessageHeader(MqttMessageHeaders.ResponseTopic, "topic/1")],
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new MqttMessageIdentifier("client1", "1234"));

        string? responseTopic = envelope.GetMqttResponseTopic();

        responseTopic.ShouldBe("topic/1");
    }

    [Fact]
    public void GetMqttResponseTopic_ShouldReturnNull_WhenHeaderNotSet()
    {
        InboundEnvelope envelope = new(
            null,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new MqttMessageIdentifier("client1", "1234"));

        string? responseTopic = envelope.GetMqttResponseTopic();

        responseTopic.ShouldBeNull();
    }

    [Fact]
    public void SetMqttResponseTopic_ShouldSetHeader()
    {
        OutboundEnvelope envelope = new(
            new TestEventOne(),
            [],
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        envelope.SetMqttResponseTopic("topic/1");

        envelope.Headers.ShouldContain(new MessageHeader(MqttMessageHeaders.ResponseTopic, "topic/1"));
    }

    [Fact]
    public void GetMqttCorrelationData_ShouldReturnCorrelationDataAsByteArray()
    {
        InboundEnvelope envelope = new(
            null,
            [new MessageHeader(MqttMessageHeaders.CorrelationData, "AQIDBA==")],
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new MqttMessageIdentifier("client1", "1234"));

        byte[]? correlationData = envelope.GetMqttCorrelationData();

        correlationData.ShouldBe([1, 2, 3, 4]);
    }

    [Fact]
    public void GetMqttCorrelationDataAsString_ShouldReturnCorrelationData()
    {
        InboundEnvelope envelope = new(
            null,
            [new MessageHeader(MqttMessageHeaders.CorrelationData, "e2NvcnJlbGF0aW9uLWRhdGF9")],
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new MqttMessageIdentifier("client1", "1234"));

        string? correlationData = envelope.GetMqttCorrelationDataAsString();

        correlationData.ShouldBe("{correlation-data}");
    }

    [Fact]
    public void GetMqttCorrelationDataAsString_ShouldReturnNull_WhenHeaderNotSet()
    {
        InboundEnvelope envelope = new(
            null,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new MqttMessageIdentifier("client1", "1234"));

        string? correlationData = envelope.GetMqttCorrelationDataAsString();

        correlationData.ShouldBeNull();
    }

    [Fact]
    public void SetMqttCorrelationData_ShouldSetHeader()
    {
        OutboundEnvelope envelope = new(
            new TestEventOne(),
            [],
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        envelope.SetMqttCorrelationData([1, 2, 3, 4]);

        envelope.Headers.ShouldContain(new MessageHeader(MqttMessageHeaders.CorrelationData, "AQIDBA=="));
    }

    [Fact]
    public void SetMqttCorrelationData_ShouldSetHeaderFromString()
    {
        OutboundEnvelope envelope = new(
            new TestEventOne(),
            [],
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        envelope.SetMqttCorrelationData("{correlation-data}");

        envelope.Headers.ShouldContain(new MessageHeader(MqttMessageHeaders.CorrelationData, "e2NvcnJlbGF0aW9uLWRhdGF9"));
    }

    [Fact]
    public void GetMqttDestinationTopic_ShouldReturnDestinationTopic()
    {
        OutboundEnvelope envelope = new(
            new TestEventOne(),
            [new MessageHeader(MqttMessageHeaders.DestinationTopic, "topic/1")],
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        string? destinationTopic = envelope.GetMqttDestinationTopic();

        destinationTopic.ShouldBe("topic/1");
    }

    [Fact]
    public void SetMqttDestinationTopic_ShouldSetHeader()
    {
        OutboundEnvelope envelope = new(
            new TestEventOne(),
            [],
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        envelope.SetMqttDestinationTopic("topic/1");

        envelope.Headers.ShouldContain(new MessageHeader(MqttMessageHeaders.DestinationTopic, "topic/1"));
    }
}
