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

public class StaticOutboundHeadersEnricherFixture
{
    [Fact]
    public void Enrich_ShouldAddStaticHeader()
    {
        OutboundEnvelope<TestEventOne> envelope = new(
            new TestEventOne(),
            null,
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>());

        StaticOutboundHeadersEnricher enricher = new("x-test", "value");

        enricher.Enrich(envelope);

        envelope.Headers.Count.ShouldBe(1);
        envelope.Headers.ShouldBe(new[] { new MessageHeader("x-test", "value") });
    }

    [Fact]
    public void Enrich_ShouldReplaceHeaderWithStaticValue()
    {
        OutboundEnvelope<TestEventOne> envelope = new(
            new TestEventOne(),
            new MessageHeaderCollection
            {
                { "x-test", "old-value" }
            },
            TestProducerEndpointConfiguration.GetDefault(),
            Substitute.For<IProducer>());

        StaticOutboundHeadersEnricher enricher = new("x-test", "value");

        enricher.Enrich(envelope);

        envelope.Headers.Count.ShouldBe(1);
        envelope.Headers.ShouldBe(new[] { new MessageHeader("x-test", "value") });
    }
}
