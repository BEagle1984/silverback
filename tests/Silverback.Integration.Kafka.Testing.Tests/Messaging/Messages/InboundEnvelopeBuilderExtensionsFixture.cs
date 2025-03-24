// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Shouldly;
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
        envelope.BrokerMessageIdentifier.ShouldBeSameAs(offset);
    }

    [Fact]
    public void WithOffset_ShouldBuildKafkaOffsetAndSetIdentifier()
    {
        InboundEnvelopeBuilder<TestEventOne> builder = new();

        builder.WithOffset("topic", 0, 42);

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.BrokerMessageIdentifier.ShouldBe(new KafkaOffset("topic", 0, 42));
    }

    [Fact]
    public void WithKafkaKey_ShouldSetKafkaKey()
    {
        InboundEnvelopeBuilder<TestEventOne> builder = new();
        const string key = "key";

        builder.WithKafkaKey(key);

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.GetKafkaKey().ShouldBe(key);
    }

    [Fact]
    public void WithKafkaTimestamp_ShouldSetKafkaTimestamp()
    {
        InboundEnvelopeBuilder<TestEventOne> builder = new();
        DateTime timestamp = new(1984, 6, 23, 2, 42, 42);

        builder.WithKafkaTimestamp(timestamp);

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.GetKafkaTimestamp().ShouldBe(timestamp);
    }
}
