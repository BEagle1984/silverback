// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.Enrichers;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Producing.Enrichers;

public class GenericOutboundHeadersEnricherFixture
{
    [Fact]
    public void Enrich_ShouldAddHeaderViaValueProvider()
    {
        OutboundEnvelope<TestEventOne> envelope = new(
            new TestEventOne { Content = "content" },
            null,
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>());

        GenericOutboundHeadersEnricher<TestEventOne> enricher = new(
            "x-test",
            envelopeToEnrich => envelopeToEnrich.Message?.Content);

        enricher.Enrich(envelope);

        envelope.Headers.Count.ShouldBe(1);
        envelope.Headers.ShouldBe(new[] { new MessageHeader("x-test", "content") });
    }

    [Fact]
    public void Enrich_ShouldAddHeaderToMatchingMessageType()
    {
        OutboundEnvelope<TestEventOne> envelopeEventOne = new(
            new TestEventOne(),
            null,
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>());
        OutboundEnvelope<TestEventTwo> envelopeEventTwo = new(
            new TestEventTwo(),
            null,
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>());

        GenericOutboundHeadersEnricher<TestEventOne> enricher = new("x-test", "value");

        enricher.Enrich(envelopeEventOne);
        enricher.Enrich(envelopeEventTwo);

        envelopeEventOne.Headers.Count.ShouldBe(1);
        envelopeEventTwo.Headers.ShouldBeEmpty();
    }

    [Fact]
    public void Enrich_ShouldAddHeaderToMatchingBaseMessageType()
    {
        OutboundEnvelope<TestEventOne> envelopeEventOne = new(
            new TestEventOne(),
            null,
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>());
        OutboundEnvelope<TestEventTwo> envelopeEventTwo = new(
            new TestEventTwo(),
            null,
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>());
        OutboundEnvelope<BinaryMessage> envelopeBinaryMessage = new(
            new BinaryMessage(),
            null,
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>());

        GenericOutboundHeadersEnricher<IIntegrationEvent> enricher = new("x-test", "value");

        enricher.Enrich(envelopeEventOne);
        enricher.Enrich(envelopeEventTwo);
        enricher.Enrich(envelopeBinaryMessage);

        envelopeEventOne.Headers.Count.ShouldBe(1);
        envelopeEventTwo.Headers.Count.ShouldBe(1);
        envelopeBinaryMessage.Headers.ShouldBeEmpty();
    }
}
