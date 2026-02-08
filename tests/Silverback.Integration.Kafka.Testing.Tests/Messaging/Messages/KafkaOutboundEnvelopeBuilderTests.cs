// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using NSubstitute;
using Shouldly;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Testing.Messaging.Messages;

public class KafkaOutboundEnvelopeBuilderTests
{
    [Fact]
    public void WithKey_ShouldSetKafkaKey()
    {
        KafkaOutboundEnvelopeBuilder<TestEventOne> builder = new();
        const string key = "key";

        builder.WithKey(key);

        IKafkaOutboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Key.ShouldBe(key);
    }

    [Fact]
    public void WithRawKey_ShouldSetRawKafkaKey()
    {
        KafkaOutboundEnvelopeBuilder<TestEventOne> builder = new();
        byte[] rawKey = [1, 2, 3];

        builder.WithRawKey(rawKey);

        IKafkaOutboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.RawKey.ShouldBe(rawKey);
    }

    [Fact]
    public void WithDestinationTopic_ShouldSetDynamicDestinationTopic()
    {
        KafkaOutboundEnvelopeBuilder<TestEventOne> builder = new();
        const string topic = "topic";

        builder.WithDestinationTopic(topic);

        IKafkaOutboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.DynamicDestinationTopic.ShouldBe(topic);
    }

    [Fact]
    public void WithDestinationPartition_ShouldSetDynamicDestinationPartition()
    {
        KafkaOutboundEnvelopeBuilder<TestEventOne> builder = new();
        const int partition = 3;

        builder.WithDestinationPartition(partition);

        IKafkaOutboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.DynamicDestinationPartition.ShouldBe(partition);
    }

    [Fact]
    public void WithContext_ShouldSetContext()
    {
        KafkaOutboundEnvelopeBuilder<TestEventOne> builder = new();
        ISilverbackContext context = Substitute.For<ISilverbackContext>();

        builder.WithContext(context);

        IKafkaOutboundEnvelope<TestEventOne> envelope = builder.Build();
        envelope.Context.ShouldBe(context);
    }
}
