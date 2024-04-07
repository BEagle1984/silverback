// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers.ArgumentResolvers;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Subscribers.ArgumentResolvers;

public class TombstoneMessageArgumentResolverFixture
{
    [Fact]
    public void CanResolve_ShouldReturnTrue_WhenParameterTypeIsTombstone()
    {
        TombstoneMessageArgumentResolver resolver = new();

        bool canResolve = resolver.CanResolve(typeof(Tombstone));

        canResolve.Should().BeTrue();
    }

    [Fact]
    public void CanResolve_ShouldReturnTrue_WhenParameterTypeIsTombstoneInterface()
    {
        TombstoneMessageArgumentResolver resolver = new();

        bool canResolve = resolver.CanResolve(typeof(ITombstone));

        canResolve.Should().BeTrue();
    }

    [Fact]
    public void CanResolve_ShouldReturnTrue_WhenParameterTypeIsTypedTombstone()
    {
        TombstoneMessageArgumentResolver resolver = new();

        bool canResolve = resolver.CanResolve(typeof(Tombstone<TestEventOne>));

        canResolve.Should().BeTrue();
    }

    [Fact]
    public void CanResolve_ShouldReturnTrue_WhenParameterTypeIsTypedTombstoneInterface()
    {
        TombstoneMessageArgumentResolver resolver = new();

        bool canResolve = resolver.CanResolve(typeof(ITombstone<TestEventOne>));

        canResolve.Should().BeTrue();
    }

    [Fact]
    public void CanResolve_ShouldReturnFalse_WhenParameterTypeIsNotTombstone()
    {
        TombstoneMessageArgumentResolver resolver = new();

        bool canResolve = resolver.CanResolve(typeof(TestEventOne));

        canResolve.Should().BeFalse();
    }

    [Fact]
    public void GetMessageType_ShouldReturnIInboundEnvelope_WhenParameterTypeIsTombstone()
    {
        TombstoneMessageArgumentResolver resolver = new();

        Type messageType = resolver.GetMessageType(typeof(Tombstone));

        messageType.Should().Be(typeof(IInboundEnvelope));
    }

    [Fact]
    public void GetMessageType_ShouldReturnIInboundEnvelope_WhenParameterTypeIsTombstoneInterface()
    {
        TombstoneMessageArgumentResolver resolver = new();

        Type messageType = resolver.GetMessageType(typeof(ITombstone));

        messageType.Should().Be(typeof(IInboundEnvelope));
    }

    [Fact]
    public void GetMessageType_ShouldReturnTypedIInboundEnvelope_WhenParameterTypeIsTypedTombstone()
    {
        TombstoneMessageArgumentResolver resolver = new();

        Type messageType = resolver.GetMessageType(typeof(Tombstone<TestEventOne>));

        messageType.Should().Be(typeof(IInboundEnvelope<TestEventOne>));
    }

    [Fact]
    public void GetMessageType_ShouldReturnTypedIInboundEnvelope_WhenParameterTypeIsTypedTombstoneInterface()
    {
        TombstoneMessageArgumentResolver resolver = new();

        Type messageType = resolver.GetMessageType(typeof(ITombstone<TestEventOne>));

        messageType.Should().Be(typeof(IInboundEnvelope<TestEventOne>));
    }

    [Fact]
    public void GetValue_ShouldReturnTombstone_WhenParameterTypeIsTombstone()
    {
        TombstoneMessageArgumentResolver resolver = new();
        IInboundEnvelope envelope = new InboundEnvelope<TestEventOne>(
            new RawInboundEnvelope(
                (byte[]?)null,
                [new MessageHeader(DefaultMessageHeaders.MessageId, "42")],
                TestConsumerEndpoint.GetDefault(),
                Substitute.For<IConsumer>(),
                new TestOffset()),
            new TestEventOne());

        object value = resolver.GetValue(envelope, typeof(Tombstone));

        value.Should().BeOfType<Tombstone>();
        value.As<Tombstone>().MessageId.Should().Be("42");
    }

    [Fact]
    public void GetValue_ShouldReturnTombstone_WhenParameterTypeIsTombstoneInterface()
    {
        TombstoneMessageArgumentResolver resolver = new();
        IInboundEnvelope envelope = new InboundEnvelope<TestEventOne>(
            new RawInboundEnvelope(
                (byte[]?)null,
                [new MessageHeader(DefaultMessageHeaders.MessageId, "42")],
                TestConsumerEndpoint.GetDefault(),
                Substitute.For<IConsumer>(),
                new TestOffset()),
            new TestEventOne());

        object value = resolver.GetValue(envelope, typeof(ITombstone));

        value.Should().BeOfType<Tombstone>();
        value.As<Tombstone>().MessageId.Should().Be("42");
    }

    [Fact]
    public void GetValue_ShouldReturnTypedTombstone_WhenParameterTypeIsTypedTombstone()
    {
        TombstoneMessageArgumentResolver resolver = new();
        IInboundEnvelope envelope = new InboundEnvelope<TestEventOne>(
            new RawInboundEnvelope(
                (byte[]?)null,
                [new MessageHeader(DefaultMessageHeaders.MessageId, "42")],
                TestConsumerEndpoint.GetDefault(),
                Substitute.For<IConsumer>(),
                new TestOffset()),
            new TestEventOne());

        object value = resolver.GetValue(envelope, typeof(Tombstone<TestEventOne>));

        value.Should().BeOfType<Tombstone<TestEventOne>>();
        value.As<Tombstone<TestEventOne>>().MessageId.Should().Be("42");
    }

    [Fact]
    public void GetValue_ShouldReturnTypedTombstone_WhenParameterTypeIsTypedTombstoneInterface()
    {
        TombstoneMessageArgumentResolver resolver = new();
        IInboundEnvelope envelope = new InboundEnvelope<TestEventOne>(
            new RawInboundEnvelope(
                (byte[]?)null,
                [new MessageHeader(DefaultMessageHeaders.MessageId, "42")],
                TestConsumerEndpoint.GetDefault(),
                Substitute.For<IConsumer>(),
                new TestOffset()),
            new TestEventOne());

        object value = resolver.GetValue(envelope, typeof(ITombstone<TestEventOne>));

        value.Should().BeOfType<Tombstone<TestEventOne>>();
        value.As<Tombstone<TestEventOne>>().MessageId.Should().Be("42");
    }
}
