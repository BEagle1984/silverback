// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using FluentAssertions;
using FluentAssertions.Extensions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Messages;

public class KafkaEnvelopeExtensionsFixture
{
    [Fact]
    public void GetKafkaKey_ShouldReturnMessageIdHeaderValue()
    {
        InboundEnvelope envelope = new(
            null,
            new MessageHeaderCollection
            {
                { DefaultMessageHeaders.MessageId, "test" }
            },
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset("a", "b"));

        string? key = envelope.GetKafkaKey();

        key.Should().Be("test");
    }

    [Fact]
    public void GetKafkaKey_ShouldReturnNull_WhenMessageIdHeaderIsNotSet()
    {
        InboundEnvelope envelope = new(
            null,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset("a", "b"));

        string? key = envelope.GetKafkaKey();

        key.Should().BeNull();
    }

    [Fact]
    public void SetKafkaKey_ShouldSetMessageIdHeader()
    {
        OutboundEnvelope envelope = new(
            new TestEventOne(),
            [],
            TestProducerEndpoint.GetDefault(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        envelope.SetKafkaKey("test");

        envelope.Headers.Should().ContainEquivalentOf(new MessageHeader(DefaultMessageHeaders.MessageId, "test"));
    }

    [Fact]
    public void GetKafkaTimestamp_ShouldReturnTimestampHeaderValue()
    {
        InboundEnvelope envelope = new(
            null,
            new MessageHeaderCollection
            {
                { KafkaMessageHeaders.Timestamp, 23.June(1984).At(02, 42, 42) }
            },
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset("a", "b"));

        DateTime? timestamp = envelope.GetKafkaTimestamp();

        timestamp.Should().Be(23.June(1984).At(02, 42, 42));
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

        timestamp.Should().BeNull();
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

        kafkaOffset.Should().Be(offset);
    }
}
