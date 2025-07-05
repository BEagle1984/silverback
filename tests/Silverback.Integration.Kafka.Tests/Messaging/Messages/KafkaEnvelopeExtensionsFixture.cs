// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Messages;

public class KafkaEnvelopeExtensionsFixture
{
    [Fact]
    public void GetKafkaKey_ShouldReturnMessageKeyHeaderValueForInboundEnvelope()
    {
        InboundEnvelope envelope = new(
            null,
            [new MessageHeader(KafkaMessageHeaders.MessageKey, "test")],
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset("a", "b"));

        string? key = envelope.GetKafkaKey();

        key.ShouldBe("test");
    }

    [Fact]
    public void GetKafkaKey_ShouldReturnMessageKeyHeaderValueForOutboundEnvelope()
    {
        OutboundEnvelope envelope = new(
            new TestEventOne(),
            [new MessageHeader(KafkaMessageHeaders.MessageKey, "test")],
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        string? key = envelope.GetKafkaKey();

        key.ShouldBe("test");
    }

    [Fact]
    public void GetKafkaKey_ShouldReturnNull_WhenMessageKeyHeaderIsNotSet()
    {
        InboundEnvelope envelope = new(
            null,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset("a", "b"));

        string? key = envelope.GetKafkaKey();

        key.ShouldBeNull();
    }

    [Fact]
    public void SetKafkaKey_ShouldSetMessageKeyHeader()
    {
        OutboundEnvelope envelope = new(
            new TestEventOne(),
            [],
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        envelope.SetKafkaKey("test");

        envelope.Headers.ShouldContain(new MessageHeader(KafkaMessageHeaders.MessageKey, "test"));
    }

    [Fact]
    public void GetKafkaTimestamp_ShouldReturnTimestampHeaderValue()
    {
        InboundEnvelope envelope = new(
            null,
            [new MessageHeader(KafkaMessageHeaders.Timestamp, new DateTime(1984, 06, 23, 02, 42, 42))],
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset("a", "b"));

        DateTime? timestamp = envelope.GetKafkaTimestamp();

        timestamp.ShouldBe(new DateTime(1984, 06, 23, 02, 42, 42));
    }

    [Fact]
    public void GetKafkaTimestamp_ShouldReturnNull_WhenTimestampHeaderIsNotSet()
    {
        InboundEnvelope envelope = new(
            null,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset("a", "b"));

        DateTime? timestamp = envelope.GetKafkaTimestamp();

        timestamp.ShouldBeNull();
    }

    [Fact]
    public void GetKafkaOffset_ShouldReturnBrokerMessageIdentifier()
    {
        KafkaOffset offset = new(new TopicPartition("test", 1), 42);
        InboundEnvelope envelope = new(
            null,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            offset);

        KafkaOffset kafkaOffset = envelope.GetKafkaOffset();

        kafkaOffset.ShouldBe(offset);
    }

    [Fact]
    public void GetKafkaDestinationTopic_ShouldReturnDestinationTopic()
    {
        OutboundEnvelope envelope = new(
            new TestEventOne(),
            [new MessageHeader(KafkaMessageHeaders.DestinationTopic, "topic/1")],
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        string? destinationTopic = envelope.GetKafkaDestinationTopic();

        destinationTopic.ShouldBe("topic/1");
    }

    [Fact]
    public void GetKafkaDestinationPartition_ShouldReturnDestinationPartition()
    {
        OutboundEnvelope envelope = new(
            new TestEventOne(),
            [new MessageHeader(KafkaMessageHeaders.DestinationPartition, 42)],
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        int? destinationPartition = envelope.GetKafkaDestinationPartition();

        destinationPartition.ShouldBe(42);
    }

    [Fact]
    public void SetKafkaDestinationTopic_ShouldSetHeader()
    {
        OutboundEnvelope envelope = new(
            new TestEventOne(),
            [],
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        envelope.SetKafkaDestinationTopic("topic/1");

        envelope.Headers.ShouldContain(new MessageHeader(KafkaMessageHeaders.DestinationTopic, "topic/1"));
    }

    [Fact]
    public void SetKafkaDestinationTopic_ShouldSetTopicAndPartitionHeaders()
    {
        OutboundEnvelope envelope = new(
            new TestEventOne(),
            [],
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        envelope.SetKafkaDestinationTopic("topic/1", 42);

        envelope.Headers.ShouldContain(new MessageHeader(KafkaMessageHeaders.DestinationTopic, "topic/1"));
        envelope.Headers.ShouldContain(new MessageHeader(KafkaMessageHeaders.DestinationPartition, 42));
    }
}
