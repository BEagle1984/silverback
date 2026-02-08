// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using Confluent.Kafka;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Messages;

public class KafkaEnvelopeExtensionsTests
{
    // TODO: Test AsKafkaInbound/Outbound

    [Fact]
    public void GetKafkaKey_ShouldReturnKeyAsStringForInboundEnvelope()
    {
        KafkaInboundEnvelope<TestEventOne> envelope = new(
            null,
            Stream.Null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new KafkaOffset("topic", 1, 1));

        envelope.SetKey("test");

        string? key = envelope.GetKafkaKey();

        key.ShouldBe("test");
    }

    [Fact]
    public void GetKafkaKey_ShouldReturnKeyAsStringForOutboundEnvelope()
    {
        KafkaOutboundEnvelope<object> envelope = new(
            new TestEventOne(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        envelope.SetKey("test");

        string? key = envelope.GetKafkaKey();

        key.ShouldBe("test");
    }

    [Fact]
    public void GetKafkaKey_ShouldReturnKeyForInboundEnvelope()
    {
        KafkaInboundEnvelope<TestEventOne> envelope = new(
            null,
            Stream.Null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new KafkaOffset("topic", 1, 1));

        envelope.SetKey(123);

        int? key = envelope.GetKafkaKey<int>();

        key.ShouldBe(123);
    }

    [Fact]
    public void GetKafkaKey_ShouldReturnKeyForOutboundEnvelope()
    {
        KafkaOutboundEnvelope<object> envelope = new(
            new TestEventOne(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        envelope.SetKey(123);

        int? key = envelope.GetKafkaKey<int>();

        key.ShouldBe(123);
    }

    [Fact]
    public void GetKafkaKey_ShouldThrowForInboundEnvelope_WhenTypeMismatch()
    {
        KafkaInboundEnvelope<TestEventOne> envelope = new(
            null,
            Stream.Null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new KafkaOffset("topic", 1, 1));

        envelope.SetKey(123);

        Action act = () => envelope.GetKafkaKey<string>();

        ArgumentException exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldStartWith("The instance must be of type Silverback.Messaging.Messages.IKafkaInboundEnvelope");
    }

    [Fact]
    public void GetKafkaKey_ShouldThrowForOutboundEnvelope_WhenTypeMismatch()
    {
        KafkaOutboundEnvelope<object> envelope = new(
            new TestEventOne(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        envelope.SetKey(123);

        Action act = () => envelope.GetKafkaKey<string>();

        ArgumentException exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldStartWith("The instance must be of type Silverback.Messaging.Messages.IKafkaOutboundEnvelope");
    }

    [Fact]
    public void GetKafkaKey_ShouldReturnNullForInboundEnvelope_WhenKeyIsNotSet()
    {
        KafkaInboundEnvelope<TestEventOne> envelope = new(
            null,
            Stream.Null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new KafkaOffset("topic", 1, 1));

        string? key = envelope.GetKafkaKey();

        key.ShouldBeNull();
    }

    [Fact]
    public void GetKafkaKey_ShouldReturnNullForOutboundEnvelope_WhenKeyIsNotSet()
    {
        KafkaOutboundEnvelope<TestEventOne> envelope = new(
            (TestEventOne?)null,
            Substitute.For<IProducer>());

        string? key = envelope.GetKafkaKey();

        key.ShouldBeNull();
    }

    [Fact]
    public void SetKafkaKey_ShouldSetKey()
    {
        KafkaOutboundEnvelope<TestEventOne> envelope = new(
            new TestEventOne(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        envelope.SetKafkaKey("test");

        envelope.GetKafkaKey().ShouldBe("test");
    }

    [Fact]
    public void SetKafkaKey_ShouldThrow_WhenKeyTypeMismatch()
    {
        KafkaOutboundEnvelope<TestEventOne> envelope = new(
            new TestEventOne(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        Action act = () => envelope.SetKafkaKey("test");

        ArgumentException exception = act.ShouldThrow<ArgumentException>();
        exception.Message.ShouldStartWith("The instance must be of type Silverback.Messaging.Messages.IKafkaOutboundEnvelope");
    }

    [Fact]
    public void GetKafkaTimestamp_ShouldReturnTimestampValue()
    {
        KafkaInboundEnvelope<TestEventOne> envelope = new(
            null,
            Stream.Null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new KafkaOffset("topic", 1, 1));

        DateTime expected = new(2025, 08, 20, 13, 35, 02, 905, DateTimeKind.Utc);
        envelope.SetTimestamp(expected);

        DateTime? timestamp = envelope.GetKafkaTimestamp();

        timestamp.ShouldBe(expected);
    }

    [Fact]
    public void GetKafkaOffset_ShouldReturnOffset()
    {
        KafkaOffset offset = new(new TopicPartition("test", 1), 42);
        KafkaInboundEnvelope<TestEventOne> envelope = new(
            null,
            Stream.Null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            offset);

        KafkaOffset kafkaOffset = envelope.GetKafkaOffset();

        kafkaOffset.ShouldBe(offset);
    }

    [Fact]
    public void GetKafkaDestinationTopic_ShouldReturnDestinationTopic()
    {
        KafkaOutboundEnvelope<TestEventOne> envelope = new(
            new TestEventOne(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        envelope.SetKafkaDestinationTopic("topic/1");

        string? destinationTopic = envelope.GetKafkaDestinationTopic();

        destinationTopic.ShouldBe("topic/1");
    }

    [Fact]
    public void GetKafkaDestinationPartition_ShouldReturnDestinationPartition()
    {
        KafkaOutboundEnvelope<TestEventOne> envelope = new(
            new TestEventOne(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        envelope.SetKafkaDestinationTopic("topic/1", 42);

        int? destinationPartition = envelope.GetKafkaDestinationPartition();

        destinationPartition.ShouldBe(42);
    }

    [Fact]
    public void SetKafkaDestinationTopic_ShouldSetTopic()
    {
        KafkaOutboundEnvelope<TestEventOne> envelope = new(
            new TestEventOne(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        envelope.SetKafkaDestinationTopic("topic/1");

        envelope.GetKafkaDestinationTopic().ShouldBe("topic/1");
    }

    [Fact]
    public void SetKafkaDestinationTopic_ShouldSetTopicAndPartition()
    {
        KafkaOutboundEnvelope<TestEventOne> envelope = new(
            new TestEventOne(),
            Substitute.For<IProducer>(),
            new SilverbackContext(Substitute.For<IServiceProvider>()));

        envelope.SetKafkaDestinationTopic("topic/1", 42);

        envelope.GetKafkaDestinationTopic().ShouldBe("topic/1");
        envelope.GetKafkaDestinationPartition().ShouldBe(42);
    }
}
