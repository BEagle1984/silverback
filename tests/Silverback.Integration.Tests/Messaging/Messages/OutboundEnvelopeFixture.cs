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
        OutboundEnvelope outboundEnvelope = new(message, null, TestProducerEndpointConfiguration.GetDefault(), Substitute.For<IProducer>());

        outboundEnvelope.Message.Should().BeSameAs(message);
        outboundEnvelope.RawMessage.As<MemoryStream>().ToArray().Should().BeEquivalentTo(message);
    }

    [Fact]
    public void Constructor_ShouldSetRawMessageFromStream()
    {
        MemoryStream stream = new([1, 2, 3]);
        OutboundEnvelope outboundEnvelope = new(stream, null, TestProducerEndpointConfiguration.GetDefault(), Substitute.For<IProducer>());

        outboundEnvelope.Message.Should().BeSameAs(stream);
        outboundEnvelope.RawMessage.Should().BeSameAs(stream);
    }

    [Fact]
    public void MessageType_ShouldReturnType_WhenMessageIsNotNull()
    {
        OutboundEnvelope envelope = new(
            new TestEventOne(),
            null,
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>());

        envelope.MessageType.Should().Be(typeof(TestEventOne));
    }

    [Fact]
    public void MessageType_ShouldReturnObject_WhenMessageIsNull()
    {
        OutboundEnvelope envelope = new(
            null,
            null,
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>());

        envelope.MessageType.Should().Be(typeof(object));
    }

    [Fact]
    public void MessageType_ShouldReturnGenericArgumentType_WhenMessageNullAndGenericArgumentProvided()
    {
        OutboundEnvelope<TestEventOne> envelope = new(
            null,
            null,
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>());

        envelope.MessageType.Should().Be(typeof(TestEventOne));
    }

    [Fact]
    public void CloneReplacingRawMessage_ShouldClone()
    {
        OutboundEnvelope<TestEventOne> envelope = new(
            new TestEventOne { Content = "old" },
            null,
            TestProducerEndpointConfiguration.GetDefault(),
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
    public void CloneReplacingMessage_ShouldChangeTypeAndClone()
    {
        OutboundEnvelope<TestEventOne> envelope = new(
            new TestEventOne { Content = "old" },
            null,
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>())
        {
            RawMessage = new MemoryStream()
        };

        IOutboundEnvelope<TestEventTwo> newEnvelope = envelope.CloneReplacingMessage(new TestEventTwo());

        newEnvelope.Should().NotBeSameAs(envelope);
        newEnvelope.Should().BeEquivalentTo(
            envelope,
            options => options
                .Excluding(e => e.Message)
                .Excluding(e => e.MessageType)
                .Excluding(e => e.RawMessage));
        newEnvelope.Message.Should().BeOfType<TestEventTwo>();
        newEnvelope.MessageType.Should().Be(typeof(TestEventTwo));
        newEnvelope.RawMessage.Should().BeNull();
    }

    [Fact]
    public void IsTombstone_ShouldReturnTrue_WhenMessageIsNull()
    {
        OutboundEnvelope<TestEventOne> envelope = new(
            null,
            null,
            TestProducerEndpointConfiguration.GetDefault(),
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
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>())
        {
            RawMessage = new MemoryStream()
        };

        envelope.IsTombstone.Should().BeTrue();
    }

    [Fact]
    public void IsTombstone_ShouldReturnFalse_WhenMessageIsNotNull()
    {
        OutboundEnvelope<TestEventOne> envelope = new(
            new TestEventOne(),
            null,
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>())
        {
            RawMessage = new MemoryStream()
        };

        envelope.IsTombstone.Should().BeFalse();
    }

    [Fact]
    public void AddHeader_ShouldAddHeader()
    {
        OutboundEnvelope<TestEventOne> envelope = new(
            null,
            null,
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>());

        envelope.AddHeader("one", "1").AddHeader("two", "2");

        envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("one", "1"));
        envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("two", "2"));
    }

    [Fact]
    public void AddOrReplaceHeader_ShouldAddHeader()
    {
        OutboundEnvelope<TestEventOne> envelope = new(
            null,
            null,
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>());

        envelope.AddOrReplaceHeader("one", "1").AddOrReplaceHeader("two", "2");

        envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("one", "1"));
        envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("two", "2"));
    }

    [Fact]
    public void AddOrReplaceHeader_ShouldReplaceHeader()
    {
        OutboundEnvelope<TestEventOne> envelope = new(
            null,
            null,
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>());

        envelope.AddOrReplaceHeader("one", "1").AddOrReplaceHeader("one", "2");

        envelope.Headers.Should().NotContainEquivalentOf(new MessageHeader("one", "1"));
        envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("one", "2"));
    }

    [Fact]
    public void AddHeaderIfNotExists_ShouldAddHeader()
    {
        OutboundEnvelope<TestEventOne> envelope = new(
            null,
            null,
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>());

        envelope.AddHeaderIfNotExists("one", "1").AddHeaderIfNotExists("two", "2");

        envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("one", "1"));
        envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("two", "2"));
    }

    [Fact]
    public void AddHeaderIfNotExists_ShouldNotReplaceHeader()
    {
        OutboundEnvelope<TestEventOne> envelope = new(
            null,
            null,
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>());

        envelope.AddHeaderIfNotExists("one", "1").AddHeaderIfNotExists("one", "2");

        envelope.Headers.Should().ContainEquivalentOf(new MessageHeader("one", "1"));
        envelope.Headers.Should().NotContainEquivalentOf(new MessageHeader("one", "2"));
    }

    [Fact]
    public void SetMessageId_ShouldSetMessageId()
    {
        OutboundEnvelope<TestEventOne> envelope = new(
            null,
            null,
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>());

        envelope.SetMessageId("one").SetMessageId("two");

        envelope.Headers.Should().ContainEquivalentOf(new MessageHeader(DefaultMessageHeaders.MessageId, "two"));
    }

    [Fact]
    public void GetMessageId_ShouldReturnHeaderValue()
    {
        OutboundEnvelope<TestEventOne> envelope = new(
            null,
            [new MessageHeader("x-message-id", "test-id")],
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>());

        envelope.GetMessageId().Should().Be("test-id");
    }

    [Fact]
    public void GetMessageId_ShouldReturnNull_WhenHeaderNotSet()
    {
        OutboundEnvelope<TestEventOne> envelope = new(
            null,
            null,
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>());

        envelope.GetMessageId().Should().BeNull();
    }
}
