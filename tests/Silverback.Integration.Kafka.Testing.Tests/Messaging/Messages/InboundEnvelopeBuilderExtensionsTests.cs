// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Shouldly;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Testing.Messaging.Messages;

public class InboundEnvelopeBuilderExtensionsTests
{
    [Fact]
    public void WithKafkaOffset_ShouldSetIdentifier()
    {
        InboundEnvelopeBuilder<TestEventOne> builder = new();
        KafkaOffset offset = new("topic", 0, 42);

        builder.WithKafkaOffset(offset);

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.BrokerMessageIdentifier.ShouldBeSameAs(offset);
    }

    [Fact]
    public void WithKafkaOffset_ShouldBuildKafkaOffsetAndSetIdentifier()
    {
        InboundEnvelopeBuilder<TestEventOne> builder = new();

        builder.WithKafkaOffset("topic", 0, 42);

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
        DateTime timestamp = new(1984, 6, 23, 2, 42, 42, DateTimeKind.Utc);

        builder.WithKafkaTimestamp(timestamp);

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.GetKafkaTimestamp().ShouldBe(timestamp);
    }

    [Fact]
    public void WithKafkaTopic_ShouldSetTopicAndPartition()
    {
        InboundEnvelopeBuilder<TestEventOne> builder = new();

        builder.WithKafkaTopic("topic", 42);

        IInboundEnvelope<TestEventOne> envelope = builder.Build();
        KafkaConsumerEndpoint kafkaEndpoint = envelope.Endpoint.ShouldBeOfType<KafkaConsumerEndpoint>();
        kafkaEndpoint.TopicPartition.Topic.ShouldBe("topic");
        kafkaEndpoint.TopicPartition.Partition.Value.ShouldBe(42);
        kafkaEndpoint.Configuration.ShouldBeOfType<KafkaConsumerEndpointConfiguration>();
    }
}
