// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.Enrichers;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Outbound.Enrichers
{
    public class GenericOutboundHeadersEnricherTests
    {
        [Fact]
        public void Enrich_StaticValues_HeaderAdded()
        {
            var envelope = EnvelopeFactory.Create(
                new TestEventOne(),
                null,
                TestProducerEndpoint.GetDefault());

            var enricher = new GenericOutboundHeadersEnricher("x-test", "value");

            enricher.Enrich(envelope);

            envelope.Headers.Should().HaveCount(1);
            envelope.Headers.Should().BeEquivalentTo(new MessageHeader("x-test", "value"));
        }

        [Fact]
        public void Enrich_StaticValues_HeaderReplaced()
        {
            var envelope = EnvelopeFactory.Create(
                new TestEventOne(),
                new MessageHeaderCollection
                {
                    { "x-test", "old-value" }
                },
                TestProducerEndpoint.GetDefault());

            var enricher = new GenericOutboundHeadersEnricher("x-test", "value");

            enricher.Enrich(envelope);

            envelope.Headers.Should().HaveCount(1);
            envelope.Headers.Should().BeEquivalentTo(new MessageHeader("x-test", "value"));
        }

        [Fact]
        public void Enrich_SpecificMessageType_HeaderAddedToMessagesOfMatchingType()
        {
            var envelopeEventOne = EnvelopeFactory.Create(
                new TestEventOne(),
                null,
                TestProducerEndpoint.GetDefault());
            var envelopeEventTwo = EnvelopeFactory.Create(
                new TestEventTwo(),
                null,
                TestProducerEndpoint.GetDefault());

            var enricher = new GenericOutboundHeadersEnricher<TestEventOne>("x-test", "value");

            enricher.Enrich(envelopeEventOne);
            enricher.Enrich(envelopeEventTwo);

            envelopeEventOne.Headers.Should().HaveCount(1);
            envelopeEventTwo.Headers.Should().BeEmpty();
        }

        [Fact]
        public void Enrich_SpecificBaseMessageType_HeaderAddedToMessagesOfMatchingType()
        {
            var envelopeEventOne = EnvelopeFactory.Create(
                new TestEventOne(),
                null,
                TestProducerEndpoint.GetDefault());
            var envelopeEventTwo = EnvelopeFactory.Create(
                new TestEventTwo(),
                null,
                TestProducerEndpoint.GetDefault());
            var envelopeBinaryMessage = EnvelopeFactory.Create(
                new BinaryMessage(),
                null,
                TestProducerEndpoint.GetDefault());

            var enricher = new GenericOutboundHeadersEnricher<IIntegrationEvent>("x-test", "value");

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
            var envelope = EnvelopeFactory.Create(
                new TestEventOne { Content = "content" },
                null,
                TestProducerEndpoint.GetDefault());

            var enricher = new GenericOutboundHeadersEnricher<TestEventOne>(
                "x-test",
                envelopeToEnrich => envelopeToEnrich.Message?.Content);

            enricher.Enrich(envelope);

            envelope.Headers.Should().HaveCount(1);
            envelope.Headers.Should().BeEquivalentTo(new MessageHeader("x-test", "content"));
        }
    }
}
