// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Messages;

public class InboundEnvelopeTests
{
    [Fact]
    public void Constructor_ShouldNotThrow_WhenMessageIsNull()
    {
        TestInboundEnvelope<object> envelope = new(
            null,
            null,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset("a", "b"));

        envelope.ShouldNotBeNull();
    }

    [Fact]
    public void MessageType_ShouldReturnType_WhenMessageIsNotNull()
    {
        TestInboundEnvelope<TestEventOne> envelope = new(
            new TestEventOne(),
            null,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset("a", "b"));

        envelope.MessageType.ShouldBe(typeof(TestEventOne));
    }

    [Fact]
    public void MessageType_ShouldReturnObject_WhenMessageIsNull()
    {
        TestInboundEnvelope<object> envelope = new(
            null,
            null,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset("a", "b"));

        envelope.MessageType.ShouldBe(typeof(object));
    }

    [Fact]
    public void MessageType_ShouldReturnGenericArgumentType_WhenMessageNullAndGenericArgumentProvided()
    {
        TestInboundEnvelope<object> envelope = new(
            null,
            null,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset("a", "b"));

        envelope.MessageType.ShouldBe(typeof(TestEventOne));
    }

    [Fact]
    public void IsTombstone_ShouldReturnTrue_WhenMessageIsNull()
    {
        TestInboundEnvelope<object> envelope = new(
            null,
            Stream.Null,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset("a", "b"));

        envelope.IsTombstone.ShouldBeTrue();
    }

    [Fact]
    public void IsTombstone_ShouldReturnTrue_WhenMessageIsTombstone()
    {
        TestInboundEnvelope<object> envelope = new(
            new Tombstone("42"),
            null,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset("a", "b"));

        envelope.IsTombstone.ShouldBeTrue();
    }

    [Fact]
    public void IsTombstone_ShouldReturnFalse_WhenMessageIsNotNull()
    {
        TestInboundEnvelope<object> envelope = new(
            new TestEventOne(),
            null,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset("a", "b"));

        envelope.IsTombstone.ShouldBeFalse();
    }

    [Fact]
    public void CloneReplacingRawMessage_ShouldClone()
    {
        TestInboundEnvelope<object> envelope = new(
            null,
            new MemoryStream(),
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset("a", "b"));

        IInboundEnvelope newEnvelope = envelope.CloneReplacingRawMessage(new MemoryStream());

        newEnvelope.ShouldNotBeSameAs(envelope);
        newEnvelope.RawMessage.ShouldNotBeSameAs(envelope.Message);
        newEnvelope.Headers.ShouldBe(envelope.Headers);
        newEnvelope.Consumer.ShouldBeSameAs(envelope.Consumer);
        newEnvelope.BrokerMessageIdentifier.ShouldBeSameAs(envelope.BrokerMessageIdentifier);
    }
}
