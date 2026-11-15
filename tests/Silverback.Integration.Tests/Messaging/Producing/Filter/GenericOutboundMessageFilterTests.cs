// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.Filter;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Producing.Filter;

public class GenericOutboundMessageFilterTests
{
    [Fact]
    public async Task ShouldProduceAsync_ShouldEvaluateMessageFilter()
    {
        OutboundEnvelope<TestEventOne> envelope1 = new(
            new TestEventOne { Content = "yes" },
            null,
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>());
        OutboundEnvelope<TestEventOne> envelope2 = new(
            new TestEventOne { Content = "no" },
            null,
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>());

        GenericOutboundMessageFilter<TestEventOne> filter = new(testEventOne => testEventOne?.Content == "yes");

        (await filter.ShouldProduceAsync(envelope1, CancellationToken.None)).ShouldBeTrue();
        (await filter.ShouldProduceAsync(envelope2, CancellationToken.None)).ShouldBeFalse();
    }

    [Fact]
    public async Task ShouldProduceAsync_ShouldEvaluateEnvelopeFilter()
    {
        OutboundEnvelope<TestEventOne> envelope1 = new(
            new TestEventOne { Content = "yes" },
            null,
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>());
        OutboundEnvelope<TestEventOne> envelope2 = new(
            new TestEventOne { Content = "no" },
            null,
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>());

        GenericOutboundMessageFilter<TestEventOne> filter = new(envelope => envelope.Message?.Content == "yes");

        (await filter.ShouldProduceAsync(envelope1, CancellationToken.None)).ShouldBeTrue();
        (await filter.ShouldProduceAsync(envelope2, CancellationToken.None)).ShouldBeFalse();
    }

    [Fact]
    public async Task ShouldProduceAsync_ShouldReturnFalse_WhenMessageTypeMismatch()
    {
        OutboundEnvelope<TestEventOne> testEnvelope = new(
            new TestEventOne { Content = "yes" },
            null,
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>());

        GenericOutboundMessageFilter<TestEventTwo> filter = new(envelope => envelope.Message?.Content == "yes");

        (await filter.ShouldProduceAsync(testEnvelope, CancellationToken.None)).ShouldBeFalse();
    }
}
