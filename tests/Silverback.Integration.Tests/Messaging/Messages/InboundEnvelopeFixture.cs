﻿// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Messages;

public class InboundEnvelopeFixture
{
    [Fact]
    public void MessageType_ShouldReturnType_WhenMessageIsNotNull()
    {
        InboundEnvelope envelope = new(
            new TestEventOne(),
            null,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset("a", "b"));

        envelope.MessageType.Should().Be(typeof(TestEventOne));
    }

    [Fact]
    public void MessageType_ShouldReturnObject_WhenMessageIsNull()
    {
        InboundEnvelope envelope = new(
            null,
            null,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset("a", "b"));

        envelope.MessageType.Should().Be(typeof(object));
    }

    [Fact]
    public void MessageType_ShouldReturnGenericArgumentType_WhenMessageNullAndGenericArgumentProvided()
    {
        InboundEnvelope<TestEventOne> envelope = new(
            null,
            null,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset("a", "b"));

        envelope.MessageType.Should().Be(typeof(TestEventOne));
    }

    [Fact]
    public void IsTombstone_ShouldReturnTrue_WhenMessageIsNull()
    {
        InboundEnvelope envelope = new(
            null,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset("a", "b"));

        envelope.IsTombstone.Should().BeTrue();
    }

    [Fact]
    public void IsTombstone_ShouldReturnTrue_WhenMessageIsTombstone()
    {
        InboundEnvelope envelope = new(
            new Tombstone("42"),
            null,
            null,
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset("a", "b"));

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
