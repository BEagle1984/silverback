// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Producing.Filter;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Producing.Filter;

public class GenericOutboundMessageFilterTests
{
    [Fact]
    public void ShouldProduce_ShouldEvaluateMessageFilter()
    {
        TestOutboundEnvelope<TestEventOne> envelope1 = new(
            new TestEventOne { Content = "yes" },
            Substitute.For<IProducer>());
        TestOutboundEnvelope<TestEventOne> envelope2 = new(
            new TestEventOne { Content = "no" },
            Substitute.For<IProducer>());

        GenericOutboundMessageFilter<TestEventOne> filter = new(testEventOne => testEventOne?.Content == "yes");

        filter.ShouldProduce(envelope1).ShouldBeTrue();
        filter.ShouldProduce(envelope2).ShouldBeFalse();
    }

    [Fact]
    public void ShouldProduce_ShouldEvaluateEnvelopeFilter()
    {
        TestOutboundEnvelope<TestEventOne> envelope1 = new(
            new TestEventOne { Content = "yes" },
            Substitute.For<IProducer>());
        TestOutboundEnvelope<TestEventOne> envelope2 = new(
            new TestEventOne { Content = "no" },
            Substitute.For<IProducer>());

        GenericOutboundMessageFilter<TestEventOne> filter = new(envelope => envelope.Message?.Content == "yes");

        filter.ShouldProduce(envelope1).ShouldBeTrue();
        filter.ShouldProduce(envelope2).ShouldBeFalse();
    }

    [Fact]
    public void ShouldProduce_ShouldReturnFalse_WhenMessageTypeMismatch()
    {
        TestOutboundEnvelope<TestEventOne> testEnvelope = new(
            new TestEventOne { Content = "yes" },
            Substitute.For<IProducer>());

        GenericOutboundMessageFilter<TestEventTwo> filter = new(envelope => envelope.Message?.Content == "yes");

        filter.ShouldProduce(testEnvelope).ShouldBeFalse();
    }
}
