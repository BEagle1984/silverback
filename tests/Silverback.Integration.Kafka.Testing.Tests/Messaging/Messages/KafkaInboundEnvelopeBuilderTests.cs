// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Testing.Messaging.Messages;

public class KafkaInboundEnvelopeBuilderTests
{
    [Fact]
    public void WithOffset_ShouldSetIdentifierAndOffset()
    {
        KafkaInboundEnvelopeBuilder<TestEventOne, string> builder = new();
        KafkaOffset offset = new("topic", 0, 42);

        builder.WithOffset(offset);

        IKafkaInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.BrokerMessageIdentifier.ShouldBeSameAs(offset);
        envelope.Offset.ShouldBe(offset);
    }

    [Fact]
    public void WithOffset_ShouldBuildKafkaOffsetAndSetIdentifierAndOffset()
    {
        KafkaInboundEnvelopeBuilder<TestEventOne, string> builder = new();

        builder.WithOffset("topic", 0, 42);

        IKafkaInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.BrokerMessageIdentifier.ShouldBe(new KafkaOffset("topic", 0, 42));
        envelope.Offset.ShouldBe(new KafkaOffset("topic", 0, 42));
    }

    [Fact]
    public void WithKey_ShouldSetKafkaKey()
    {
        KafkaInboundEnvelopeBuilder<TestEventOne, string> builder = new();
        const string key = "key";

        builder.WithKey(key);

        IKafkaInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Key.ShouldBe(key);
    }

    [Fact]
    public void WithRawKey_ShouldSetRawKafkaKey()
    {
        KafkaInboundEnvelopeBuilder<TestEventOne, string> builder = new();
        byte[] rawKey = [1, 2, 3];

        builder.WithRawKey(rawKey);

        IKafkaInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.RawKey.ShouldBe(rawKey);
    }

    [Fact]
    public void WithTimestamp_ShouldSetTimestamp()
    {
        KafkaInboundEnvelopeBuilder<TestEventOne, string> builder = new();
        DateTime timestamp = new(1984, 6, 23, 2, 42, 42, DateTimeKind.Utc);

        builder.WithTimestamp(timestamp);

        IKafkaInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Timestamp.ShouldBe(timestamp);
    }
}
