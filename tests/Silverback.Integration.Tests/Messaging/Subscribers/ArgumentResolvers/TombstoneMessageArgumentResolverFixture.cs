// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using NSubstitute;
using Shouldly;
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

        canResolve.ShouldBeTrue();
    }

    [Fact]
    public void CanResolve_ShouldReturnTrue_WhenParameterTypeIsTombstoneInterface()
    {
        TombstoneMessageArgumentResolver resolver = new();

        bool canResolve = resolver.CanResolve(typeof(ITombstone));

        canResolve.ShouldBeTrue();
    }

    [Fact]
    public void CanResolve_ShouldReturnTrue_WhenParameterTypeIsTypedTombstone()
    {
        TombstoneMessageArgumentResolver resolver = new();

        bool canResolve = resolver.CanResolve(typeof(Tombstone<TestEventOne>));

        canResolve.ShouldBeTrue();
    }

    [Fact]
    public void CanResolve_ShouldReturnTrue_WhenParameterTypeIsTypedTombstoneInterface()
    {
        TombstoneMessageArgumentResolver resolver = new();

        bool canResolve = resolver.CanResolve(typeof(ITombstone<TestEventOne>));

        canResolve.ShouldBeTrue();
    }

    [Fact]
    public void CanResolve_ShouldReturnFalse_WhenParameterTypeIsNotTombstone()
    {
        TombstoneMessageArgumentResolver resolver = new();

        bool canResolve = resolver.CanResolve(typeof(TestEventOne));

        canResolve.ShouldBeFalse();
    }

    [Fact]
    public void GetMessageType_ShouldReturnIInboundEnvelope_WhenParameterTypeIsTombstone()
    {
        TombstoneMessageArgumentResolver resolver = new();

        Type messageType = resolver.GetMessageType(typeof(Tombstone));

        messageType.ShouldBe(typeof(IInboundEnvelope));
    }

    [Fact]
    public void GetMessageType_ShouldReturnIInboundEnvelope_WhenParameterTypeIsTombstoneInterface()
    {
        TombstoneMessageArgumentResolver resolver = new();

        Type messageType = resolver.GetMessageType(typeof(ITombstone));

        messageType.ShouldBe(typeof(IInboundEnvelope));
    }

    [Fact]
    public void GetMessageType_ShouldReturnTypedIInboundEnvelope_WhenParameterTypeIsTypedTombstone()
    {
        TombstoneMessageArgumentResolver resolver = new();

        Type messageType = resolver.GetMessageType(typeof(Tombstone<TestEventOne>));

        messageType.ShouldBe(typeof(IInboundEnvelope<TestEventOne>));
    }

    [Fact]
    public void GetMessageType_ShouldReturnTypedIInboundEnvelope_WhenParameterTypeIsTypedTombstoneInterface()
    {
        TombstoneMessageArgumentResolver resolver = new();

        Type messageType = resolver.GetMessageType(typeof(ITombstone<TestEventOne>));

        messageType.ShouldBe(typeof(IInboundEnvelope<TestEventOne>));
    }

    [Fact]
    public void GetValue_ShouldReturnTombstone_WhenParameterTypeIsTombstone()
    {
        TombstoneMessageArgumentResolver resolver = new();
        IInboundEnvelope envelope = new InboundEnvelope<TestEventOne>(
            new RawInboundEnvelope(
                (byte[]?)null,
                [new MessageHeader(DefaultMessageHeaders.MessageKey, "42")],
                TestConsumerEndpoint.GetDefault(),
                Substitute.For<IConsumer>(),
                new TestOffset()),
            new TestEventOne());

        object value = resolver.GetValue(envelope, typeof(Tombstone));

        Tombstone tombstone = value.ShouldBeOfType<Tombstone>();
        tombstone.MessageKey.ShouldBe("42");
    }

    [Fact]
    public void GetValue_ShouldReturnTombstone_WhenParameterTypeIsTombstoneInterface()
    {
        TombstoneMessageArgumentResolver resolver = new();
        IInboundEnvelope envelope = new InboundEnvelope<TestEventOne>(
            new RawInboundEnvelope(
                (byte[]?)null,
                [new MessageHeader(DefaultMessageHeaders.MessageKey, "42")],
                TestConsumerEndpoint.GetDefault(),
                Substitute.For<IConsumer>(),
                new TestOffset()),
            new TestEventOne());

        object value = resolver.GetValue(envelope, typeof(ITombstone));

        Tombstone tombstone = value.ShouldBeOfType<Tombstone>();
        tombstone.MessageKey.ShouldBe("42");
    }

    [Fact]
    public void GetValue_ShouldReturnTypedTombstone_WhenParameterTypeIsTypedTombstone()
    {
        TombstoneMessageArgumentResolver resolver = new();
        IInboundEnvelope envelope = new InboundEnvelope<TestEventOne>(
            new RawInboundEnvelope(
                (byte[]?)null,
                [new MessageHeader(DefaultMessageHeaders.MessageKey, "42")],
                TestConsumerEndpoint.GetDefault(),
                Substitute.For<IConsumer>(),
                new TestOffset()),
            new TestEventOne());

        object value = resolver.GetValue(envelope, typeof(Tombstone<TestEventOne>));

        Tombstone<TestEventOne> tombstone = value.ShouldBeOfType<Tombstone<TestEventOne>>();
        tombstone.MessageKey.ShouldBe("42");
    }

    [Fact]
    public void GetValue_ShouldReturnTypedTombstone_WhenParameterTypeIsTypedTombstoneInterface()
    {
        TombstoneMessageArgumentResolver resolver = new();
        IInboundEnvelope envelope = new InboundEnvelope<TestEventOne>(
            new RawInboundEnvelope(
                (byte[]?)null,
                [new MessageHeader(DefaultMessageHeaders.MessageKey, "42")],
                TestConsumerEndpoint.GetDefault(),
                Substitute.For<IConsumer>(),
                new TestOffset()),
            new TestEventOne());

        object value = resolver.GetValue(envelope, typeof(ITombstone<TestEventOne>));

        Tombstone<TestEventOne> tombstone = value.ShouldBeOfType<Tombstone<TestEventOne>>();
        tombstone.MessageKey.ShouldBe("42");
    }
}
