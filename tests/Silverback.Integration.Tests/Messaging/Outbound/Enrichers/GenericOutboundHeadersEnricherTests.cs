// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.Enrichers;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Outbound.Enrichers;

public class GenericOutboundHeadersEnricherTests
{
    [Fact]
    public void Enrich_StaticValues_HeaderAdded()
    {
        OutboundEnvelope<TestEventOne> envelope = new(
            new TestEventOne(),
            null,
            TestProducerEndpoint.GetDefault(),
            Substitute.For<IProducer>());

        GenericOutboundHeadersEnricher enricher = new("x-test", "value");

        enricher.Enrich(envelope);

        envelope.Headers.Should().HaveCount(1);
        envelope.Headers.Should().BeEquivalentTo(new[] { new MessageHeader("x-test", "value") });
    }

    [Fact]
    public void Enrich_StaticValues_HeaderReplaced()
    {
        OutboundEnvelope<TestEventOne> envelope = new(
            new TestEventOne(),
            new MessageHeaderCollection
            {
                { "x-test", "old-value" }
            },
            TestProducerEndpoint.GetDefault(),
            Substitute.For<IProducer>());

        GenericOutboundHeadersEnricher enricher = new("x-test", "value");

        enricher.Enrich(envelope);

        envelope.Headers.Should().HaveCount(1);
        envelope.Headers.Should().BeEquivalentTo(new[] { new MessageHeader("x-test", "value") });
    }

    [Fact]
    public void Enrich_SpecificMessageType_HeaderAddedToMessagesOfMatchingType()
    {
        OutboundEnvelope<TestEventOne> envelopeEventOne = new(
            new TestEventOne(),
            null,
            TestProducerEndpoint.GetDefault(),
            Substitute.For<IProducer>());
        OutboundEnvelope<TestEventTwo> envelopeEventTwo = new(
            new TestEventTwo(),
            null,
            TestProducerEndpoint.GetDefault(),
            Substitute.For<IProducer>());

        GenericOutboundHeadersEnricher<TestEventOne> enricher = new("x-test", "value");

        enricher.Enrich(envelopeEventOne);
        enricher.Enrich(envelopeEventTwo);

        envelopeEventOne.Headers.Should().HaveCount(1);
        envelopeEventTwo.Headers.Should().BeEmpty();
    }

    [Fact]
    public void Enrich_SpecificBaseMessageType_HeaderAddedToMessagesOfMatchingType()
    {
        OutboundEnvelope<TestEventOne> envelopeEventOne = new(
            new TestEventOne(),
            null,
            TestProducerEndpoint.GetDefault(),
            Substitute.For<IProducer>());
        OutboundEnvelope<TestEventTwo> envelopeEventTwo = new(
            new TestEventTwo(),
            null,
            TestProducerEndpoint.GetDefault(),
            Substitute.For<IProducer>());
        OutboundEnvelope<BinaryMessage> envelopeBinaryMessage = new(
            new BinaryMessage(),
            null,
            TestProducerEndpoint.GetDefault(),
            Substitute.For<IProducer>());

        GenericOutboundHeadersEnricher<IIntegrationEvent> enricher = new("x-test", "value");

        enricher.Enrich(envelopeEventOne);
        enricher.Enrich(envelopeEventTwo);
        enricher.Enrich(envelopeBinaryMessage);

        envelopeEventOne.Headers.Should().HaveCount(1);
        envelopeEventTwo.Headers.Should().HaveCount(1);
        envelopeBinaryMessage.Headers.Should().BeEmpty();
    }

    [Fact]
    public void Enrich_ValueProvider_HeaderAdded()
    {
        OutboundEnvelope<TestEventOne> envelope = new(
            new TestEventOne { Content = "content" },
            null,
            TestProducerEndpoint.GetDefault(),
            Substitute.For<IProducer>());

        GenericOutboundHeadersEnricher<TestEventOne> enricher = new(
            "x-test",
            envelopeToEnrich => envelopeToEnrich.Message?.Content);

        enricher.Enrich(envelope);

        envelope.Headers.Should().HaveCount(1);
        envelope.Headers.Should().BeEquivalentTo(new[] { new MessageHeader("x-test", "content") });
    }
}
