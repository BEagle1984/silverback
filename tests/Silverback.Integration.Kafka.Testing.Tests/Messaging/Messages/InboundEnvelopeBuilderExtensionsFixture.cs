// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using FluentAssertions.Extensions;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Testing.Messaging.Messages;

public class InboundEnvelopeBuilderExtensionsFixture
{
    [Fact]
    public void WithOffset_ShouldSetIdentifier()
    {
        InboundEnvelopeBuilder<TestEventOne> builder = new();
        KafkaOffset offset = new("topic", 0, 42);

        builder.WithOffset(offset);

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.BrokerMessageIdentifier.Should().BeSameAs(offset);
    }

    [Fact]
    public void WithOffset_ShouldBuildKafkaOffsetAndSetIdentifier()
    {
        InboundEnvelopeBuilder<TestEventOne> builder = new();

        builder.WithOffset("topic", 0, 42);

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.BrokerMessageIdentifier.Should().BeOfType<KafkaOffset>()
            .Which.Should().BeEquivalentTo(new KafkaOffset("topic", 0, 42));
    }

    [Fact]
    public void WithKafkaKey_ShouldSetKafkaKey()
    {
        InboundEnvelopeBuilder<TestEventOne> builder = new();
        const string key = "key";

        builder.WithKafkaKey(key);

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.GetKafkaKey().Should().Be(key);
    }

    [Fact]
    public void WithKafkaTimestamp_ShouldSetKafkaTimestamp()
    {
        InboundEnvelopeBuilder<TestEventOne> builder = new();
        DateTime timestamp = 23.June(1984).At(02, 42, 42);

        builder.WithKafkaTimestamp(timestamp);

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.GetKafkaTimestamp().Should().Be(timestamp);
    }
}
