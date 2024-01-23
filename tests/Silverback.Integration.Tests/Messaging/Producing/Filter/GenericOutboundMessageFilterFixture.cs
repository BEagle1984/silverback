// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.Filter;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Producing.Filter;

public class GenericOutboundMessageFilterFixture
{
    [Fact]
    public void ShouldProduce_ShouldEvaluateMessageFilter()
    {
        OutboundEnvelope<TestEventOne> envelope1 = new(
            new TestEventOne { Content = "yes" },
            null,
            TestProducerEndpoint.GetDefault(),
            Substitute.For<IProducer>());
        OutboundEnvelope<TestEventOne> envelope2 = new(
            new TestEventOne { Content = "no" },
            null,
            TestProducerEndpoint.GetDefault(),
            Substitute.For<IProducer>());

        GenericOutboundMessageFilter<TestEventOne> filter = new(testEventOne => testEventOne?.Content == "yes");

        filter.ShouldProduce(envelope1).Should().BeTrue();
        filter.ShouldProduce(envelope2).Should().BeFalse();
    }

    [Fact]
    public void ShouldProduce_ShouldEvaluateEnvelopeFilter()
    {
        OutboundEnvelope<TestEventOne> envelope1 = new(
            new TestEventOne { Content = "yes" },
            null,
            TestProducerEndpoint.GetDefault(),
            Substitute.For<IProducer>());
        OutboundEnvelope<TestEventOne> envelope2 = new(
            new TestEventOne { Content = "no" },
            null,
            TestProducerEndpoint.GetDefault(),
            Substitute.For<IProducer>());

        GenericOutboundMessageFilter<TestEventOne> filter = new(envelope => envelope.Message?.Content == "yes");

        filter.ShouldProduce(envelope1).Should().BeTrue();
        filter.ShouldProduce(envelope2).Should().BeFalse();
    }

    [Fact]
    public void ShouldProduce_ShouldReturnFalse_WhenMessageTypeMismatch()
    {
        OutboundEnvelope<TestEventOne> testEnvelope = new(
            new TestEventOne { Content = "yes" },
            null,
            TestProducerEndpoint.GetDefault(),
            Substitute.For<IProducer>());

        GenericOutboundMessageFilter<TestEventTwo> filter = new(envelope => envelope.Message?.Content == "yes");

        filter.ShouldProduce(testEnvelope).Should().BeFalse();
    }
}
