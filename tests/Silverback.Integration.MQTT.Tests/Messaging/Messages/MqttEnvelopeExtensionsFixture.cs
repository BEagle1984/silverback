// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Messages;

public class MqttEnvelopeExtensionsFixture
{
    [Fact]
    public void GetResponseTopic_ShouldReturnResponseTopic()
    {
        InboundEnvelope envelope = new(
            null,
            [new MessageHeader(MqttMessageHeaders.ResponseTopic, "topic/1")],
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new MqttMessageIdentifier("client1", "1234"));

        string? responseTopic = envelope.GetResponseTopic();

        responseTopic.Should().Be("topic/1");
    }

    [Fact]
    public void GetResponseTopic_ShouldReturnNull_WhenHeaderNotSet()
    {
        InboundEnvelope envelope = new(
            null,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new MqttMessageIdentifier("client1", "1234"));

        string? responseTopic = envelope.GetResponseTopic();

        responseTopic.Should().BeNull();
    }

    [Fact]
    public void SetResponseTopic_ShouldSetHeader()
    {
        OutboundEnvelope envelope = new(
            new TestEventOne(),
            [],
            TestProducerEndpoint.GetDefault(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        envelope.SetResponseTopic("topic/1");

        envelope.Headers.Should().ContainEquivalentOf(new MessageHeader(MqttMessageHeaders.ResponseTopic, "topic/1"));
    }

    [Fact]
    public void GetCorrelationData_ShouldReturnCorrelationDataAsByteArray()
    {
        InboundEnvelope envelope = new(
            null,
            [new MessageHeader(MqttMessageHeaders.CorrelationData, "AQIDBA==")],
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new MqttMessageIdentifier("client1", "1234"));

        byte[]? correlationData = envelope.GetCorrelationData();

        correlationData.Should().BeEquivalentTo(new byte[] { 1, 2, 3, 4 });
    }

    [Fact]
    public void GetCorrelationDataAsString_ShouldReturnCorrelationData()
    {
        InboundEnvelope envelope = new(
            null,
            [new MessageHeader(MqttMessageHeaders.CorrelationData, "e2NvcnJlbGF0aW9uLWRhdGF9")],
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new MqttMessageIdentifier("client1", "1234"));

        string? correlationData = envelope.GetCorrelationDataAsString();

        correlationData.Should().Be("{correlation-data}");
    }

    [Fact]
    public void GetCorrelationDataAsString_ShouldReturnNull_WhenHeaderNotSet()
    {
        InboundEnvelope envelope = new(
            null,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new MqttMessageIdentifier("client1", "1234"));

        string? correlationData = envelope.GetCorrelationDataAsString();

        correlationData.Should().BeNull();
    }

    [Fact]
    public void SetCorrelationData_ShouldSetHeader()
    {
        OutboundEnvelope envelope = new(
            new TestEventOne(),
            [],
            TestProducerEndpoint.GetDefault(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        envelope.SetCorrelationData([1, 2, 3, 4]);

        envelope.Headers.Should().ContainEquivalentOf(new MessageHeader(MqttMessageHeaders.CorrelationData, "AQIDBA=="));
    }

    [Fact]
    public void SetCorrelationData_ShouldSetHeaderFromString()
    {
        OutboundEnvelope envelope = new(
            new TestEventOne(),
            [],
            TestProducerEndpoint.GetDefault(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        envelope.SetCorrelationData("{correlation-data}");

        envelope.Headers.Should().ContainEquivalentOf(new MessageHeader(MqttMessageHeaders.CorrelationData, "e2NvcnJlbGF0aW9uLWRhdGF9"));
    }
}
