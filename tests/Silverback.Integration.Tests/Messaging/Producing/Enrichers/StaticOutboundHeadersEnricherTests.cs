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

public class StaticOutboundHeadersEnricherTests
{
    [Fact]
    public void Enrich_ShouldAddStaticHeader()
    {
        TestOutboundEnvelope<TestEventOne> envelope = new(new TestEventOne(), Substitute.For<IProducer>());
        StaticOutboundHeadersEnricher enricher = new("x-test", "value");

        enricher.Enrich(envelope);

        envelope.Headers.Count.ShouldBe(1);
        envelope.Headers.ShouldBe(new[] { new MessageHeader("x-test", "value") });
    }

    [Fact]
    public void Enrich_ShouldReplaceHeaderWithStaticValue()
    {
        TestOutboundEnvelope<TestEventOne> envelope = new(new TestEventOne(), Substitute.For<IProducer>());
        envelope.AddHeader("x-test", "old-value");
        StaticOutboundHeadersEnricher enricher = new("x-test", "new-value");

        enricher.Enrich(envelope);

        envelope.Headers.Count.ShouldBe(1);
        envelope.Headers.ShouldBe(new[] { new MessageHeader("x-test", "new-value") });
    }
}
