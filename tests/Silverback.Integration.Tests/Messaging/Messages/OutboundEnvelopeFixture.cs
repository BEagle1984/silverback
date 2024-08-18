// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Messages;

public class OutboundEnvelopeFixture
{
    [Fact]
    public void Constructor_ShouldSetRawMessageFromByteArray()
    {
        byte[] message = [1, 2, 3];
        OutboundEnvelope outboundEnvelope = new(message, null, TestProducerEndpoint.GetDefault(), Substitute.For<IProducer>());

        outboundEnvelope.Message.Should().BeSameAs(message);
        outboundEnvelope.RawMessage.As<MemoryStream>().ToArray().Should().BeEquivalentTo(message);
    }

    [Fact]
    public void Constructor_ShouldSetRawMessageFromStream()
    {
        MemoryStream stream = new([1, 2, 3]);
        OutboundEnvelope outboundEnvelope = new(stream, null, TestProducerEndpoint.GetDefault(), Substitute.For<IProducer>());

        outboundEnvelope.Message.Should().BeSameAs(stream);
        outboundEnvelope.RawMessage.Should().BeSameAs(stream);
    }

    [Fact]
    public void MessageType_ShouldReturnType_WhenMessageIsNotNull()
    {
        OutboundEnvelope envelope = new(
            new TestEventOne(),
            null,
            TestProducerEndpoint.GetDefault(),
            Substitute.For<IProducer>());

        envelope.MessageType.Should().Be(typeof(TestEventOne));
    }

    [Fact]
    public void MessageType_ShouldReturnObject_WhenMessageIsNull()
    {
        OutboundEnvelope envelope = new(
            null,
            null,
            TestProducerEndpoint.GetDefault(),
            Substitute.For<IProducer>());

        envelope.MessageType.Should().Be(typeof(object));
    }

    [Fact]
    public void MessageType_ShouldReturnGenericArgumentType_WhenMessageNullAndGenericArgumentProvided()
    {
        OutboundEnvelope<TestEventOne> envelope = new(
            null,
            null,
            TestProducerEndpoint.GetDefault(),
            Substitute.For<IProducer>());

        envelope.MessageType.Should().Be(typeof(TestEventOne));
    }

    [Fact]
    public void CloneReplacingRawMessage_ShouldClone()
    {
        OutboundEnvelope<TestEventOne> envelope = new(
            new TestEventOne { Content = "old" },
            null,
            TestProducerEndpoint.GetDefault(),
            Substitute.For<IProducer>())
        {
            RawMessage = new MemoryStream()
        };

        IOutboundEnvelope newEnvelope = envelope.CloneReplacingRawMessage(new MemoryStream());

        newEnvelope.Should().NotBeSameAs(envelope);
        newEnvelope.Should().BeEquivalentTo(envelope, options => options.Excluding(e => e.RawMessage));
        newEnvelope.RawMessage.Should().NotBeSameAs(envelope.RawMessage);
    }

    [Fact]
    public void IsTombstone_ShouldReturnTrue_WhenMessageIsNull()
    {
        OutboundEnvelope<TestEventOne> envelope = new(
            null,
            null,
            TestProducerEndpoint.GetDefault(),
            Substitute.For<IProducer>())
        {
            RawMessage = null
        };

        envelope.IsTombstone.Should().BeTrue();
    }

    [Fact]
    public void IsTombstone_ShouldReturnTrue_WhenMessageIsTombstone()
    {
        OutboundEnvelope<Tombstone<TestEventOne>> envelope = new(
            new Tombstone<TestEventOne>("key"),
            null,
            TestProducerEndpoint.GetDefault(),
            Substitute.For<IProducer>())
        {
            RawMessage = new MemoryStream()
        };

        envelope.IsTombstone.Should().BeTrue();
    }

    [Fact]
    public void IsTombstone_ShouldReturnFalse_WhenMessageIsNotNull()
    {
        InboundEnvelope envelope = new(
            new TestEventOne(),
            null,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset("a", "b"));

        envelope.IsTombstone.Should().BeFalse();
    }
}
