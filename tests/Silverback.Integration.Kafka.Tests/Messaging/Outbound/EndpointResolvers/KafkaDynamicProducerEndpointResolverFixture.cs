// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;
using NSubstitute;
using Shouldly;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Outbound.EndpointResolvers;

public class KafkaDynamicProducerEndpointResolverFixture
{
    private readonly IOutboundEnvelope<TestEventOne> _envelope = new OutboundEnvelope<TestEventOne>(
        new TestEventOne(),
        null,
        new KafkaProducerEndpointConfiguration(),
        Substitute.For<IProducer>());

    [Fact]
    public void GetEndpoint_ShouldReturnTopicAndPartitionFromEnvelopeBasedTopicNameAndPartitionFunction()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new("topic", (IOutboundEnvelope<TestEventOne> _) => 42);

        ProducerEndpoint endpoint = endpointResolver.GetEndpoint(_envelope);

        KafkaProducerEndpoint kafkaEndpoint = endpoint.ShouldBeOfType<KafkaProducerEndpoint>();
        kafkaEndpoint.TopicPartition.Topic.ShouldBe("topic");
        kafkaEndpoint.TopicPartition.Partition.ShouldBe(new Partition(42));
    }

    [Fact]
    public void GetEndpoint_ShouldReturnTopicAndPartitionFromMessageBasedTopicNameAndPartitionFunction()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new("topic", (TestEventOne? _) => 42);

        ProducerEndpoint endpoint = endpointResolver.GetEndpoint(_envelope);

        KafkaProducerEndpoint kafkaEndpoint = endpoint.ShouldBeOfType<KafkaProducerEndpoint>();
        kafkaEndpoint.TopicPartition.Topic.ShouldBe("topic");
        kafkaEndpoint.TopicPartition.Partition.ShouldBe(new Partition(42));
    }

    [Fact]
    public void GetEndpoint_ShouldReturnTopicFromEnvelopeBasedTopicNameFunction()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new((IOutboundEnvelope<TestEventOne> _) => "topic");

        ProducerEndpoint endpoint = endpointResolver.GetEndpoint(_envelope);

        KafkaProducerEndpoint kafkaEndpoint = endpoint.ShouldBeOfType<KafkaProducerEndpoint>();
        kafkaEndpoint.TopicPartition.Topic.ShouldBe("topic");
        kafkaEndpoint.TopicPartition.Partition.ShouldBe(Partition.Any);
    }

    [Fact]
    public void GetEndpoint_ShouldReturnTopicFromMessageBasedTopicNameFunction()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new((TestEventOne? _) => "topic");

        ProducerEndpoint endpoint = endpointResolver.GetEndpoint(_envelope);

        KafkaProducerEndpoint kafkaEndpoint = endpoint.ShouldBeOfType<KafkaProducerEndpoint>();
        kafkaEndpoint.TopicPartition.Topic.ShouldBe("topic");
        kafkaEndpoint.TopicPartition.Partition.ShouldBe(Partition.Any);
    }

    [Fact]
    public void GetEndpoint_ShouldReturnTopicAndPartitionFromEnvelopeBasedTopicNameFunctionAndPartitionFunction()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new((IOutboundEnvelope<TestEventOne> _) => "topic", _ => 42);

        ProducerEndpoint endpoint = endpointResolver.GetEndpoint(_envelope);

        KafkaProducerEndpoint kafkaEndpoint = endpoint.ShouldBeOfType<KafkaProducerEndpoint>();
        kafkaEndpoint.TopicPartition.Topic.ShouldBe("topic");
        kafkaEndpoint.TopicPartition.Partition.ShouldBe(new Partition(42));
    }

    [Fact]
    public void GetEndpoint_ShouldReturnTopicAndPartitionFromMessageBasedTopicNameFunctionAndPartitionFunction()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new((TestEventOne? _) => "topic", _ => 42);

        ProducerEndpoint endpoint = endpointResolver.GetEndpoint(_envelope);

        KafkaProducerEndpoint kafkaEndpoint = endpoint.ShouldBeOfType<KafkaProducerEndpoint>();
        kafkaEndpoint.TopicPartition.Topic.ShouldBe("topic");
        kafkaEndpoint.TopicPartition.Partition.ShouldBe(new Partition(42));
    }

    [Fact]
    public void GetEndpoint_ShouldReturnTopicAndPartitionFromEnvelopeBasedTopicPartitionFunction()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new((IOutboundEnvelope<TestEventOne> _) => new TopicPartition("topic", 42));

        ProducerEndpoint endpoint = endpointResolver.GetEndpoint(_envelope);

        KafkaProducerEndpoint kafkaEndpoint = endpoint.ShouldBeOfType<KafkaProducerEndpoint>();
        kafkaEndpoint.TopicPartition.Topic.ShouldBe("topic");
        kafkaEndpoint.TopicPartition.Partition.ShouldBe(new Partition(42));
    }

    [Fact]
    public void GetEndpoint_ShouldReturnTopicAndPartitionFromMessageBasedTopicPartitionFunction()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new((TestEventOne? _) => new TopicPartition("topic", 42));

        ProducerEndpoint endpoint = endpointResolver.GetEndpoint(_envelope);

        KafkaProducerEndpoint kafkaEndpoint = endpoint.ShouldBeOfType<KafkaProducerEndpoint>();
        kafkaEndpoint.TopicPartition.Topic.ShouldBe("topic");
        kafkaEndpoint.TopicPartition.Partition.ShouldBe(new Partition(42));
    }

    [Fact]
    public void GetEndpoint_ShouldReturnTopicAndPartitionFromEnvelopeBasedTopicNameFormatAndPartitionFunction()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new(
            "topic-{0}",
            (IOutboundEnvelope<TestEventOne> _) => ["123"],
            _ => 42);

        ProducerEndpoint endpoint = endpointResolver.GetEndpoint(_envelope);

        KafkaProducerEndpoint kafkaEndpoint = endpoint.ShouldBeOfType<KafkaProducerEndpoint>();
        kafkaEndpoint.TopicPartition.Topic.ShouldBe("topic-123");
        kafkaEndpoint.TopicPartition.Partition.ShouldBe(new Partition(42));
    }

    [Fact]
    public void GetEndpoint_ShouldReturnTopicAndPartitionFromMessageBasedTopicNameFormatAndPartitionFunction()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new(
            "topic-{0}",
            (TestEventOne? _) => ["123"],
            _ => 42);

        ProducerEndpoint endpoint = endpointResolver.GetEndpoint(_envelope);

        KafkaProducerEndpoint kafkaEndpoint = endpoint.ShouldBeOfType<KafkaProducerEndpoint>();
        kafkaEndpoint.TopicPartition.Topic.ShouldBe("topic-123");
        kafkaEndpoint.TopicPartition.Partition.ShouldBe(new Partition(42));
    }

    [Fact]
    public void GetEndpoint_ShouldReturnTopicAndPartitionFromResolver()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new(
            typeof(TestEndpointResolver),
            envelope => new TestEndpointResolver().GetTopicPartition(envelope));

        ProducerEndpoint endpoint = endpointResolver.GetEndpoint(_envelope);

        KafkaProducerEndpoint kafkaEndpoint = endpoint.ShouldBeOfType<KafkaProducerEndpoint>();
        kafkaEndpoint.TopicPartition.Topic.ShouldBe("topic");
        kafkaEndpoint.TopicPartition.Partition.ShouldBe(new Partition(42));
    }

    [Fact]
    public void RawName_ShouldReturnTopicNameWhenUsingEnvelopeBasedPartitionFunction()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new("topic", (IOutboundEnvelope<TestEventOne> _) => 42);

        endpointResolver.RawName.ShouldStartWith("topic");
    }

    [Fact]
    public void RawName_ShouldReturnTopicNameWhenUsingMessageBasedPartitionFunction()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new("topic", (TestEventOne? _) => 42);

        endpointResolver.RawName.ShouldStartWith("topic");
    }

    [Fact]
    public void RawName_ShouldReturnPlaceholderFromEnvelopeBasedTopicNameFunction()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new((IOutboundEnvelope<TestEventOne> _) => "topic");

        endpointResolver.RawName.ShouldStartWith("dynamic-");
    }

    [Fact]
    public void RawName_ShouldReturnPlaceholderFromMessageBasedTopicNameFunction()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new((TestEventOne? _) => "topic");

        endpointResolver.RawName.ShouldStartWith("dynamic-");
    }

    [Fact]
    public void RawName_ShouldReturnPlaceholderFromEnvelopeBasedTopicNameFunctionAndPartitionFunction()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new((IOutboundEnvelope<TestEventOne> _) => "topic", _ => 42);

        endpointResolver.RawName.ShouldStartWith("dynamic-");
    }

    [Fact]
    public void RawName_ShouldReturnPlaceholderFromMessageBasedTopicNameFunctionAndPartitionFunction()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new((TestEventOne? _) => "topic", _ => 42);

        endpointResolver.RawName.ShouldStartWith("dynamic-");
    }

    [Fact]
    public void RawName_ShouldReturnPlaceholderFromEnvelopeBasedTopicPartitionFunction()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new((IOutboundEnvelope<TestEventOne> _) => new TopicPartition("topic", 42));

        endpointResolver.RawName.ShouldStartWith("dynamic-");
    }

    [Fact]
    public void RawName_ShouldReturnPlaceholderFromMessageBasedTopicPartitionFunction()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new((TestEventOne? _) => new TopicPartition("topic", 42));

        endpointResolver.RawName.ShouldStartWith("dynamic-");
    }

    [Fact]
    public void RawName_ShouldReturnFormatStringFromEnvelopeBasedTopicFormat()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new(
            "topic-{0}",
            (IOutboundEnvelope<TestEventOne> _) => ["123"]);

        endpointResolver.RawName.ShouldStartWith("topic-{0}");
    }

    [Fact]
    public void RawName_ShouldReturnFormatStringFromMessageBasedTopicFormat()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new(
            "topic-{0}",
            (TestEventOne? _) => ["123"]);

        endpointResolver.RawName.ShouldStartWith("topic-{0}");
    }

    [Fact]
    public void RawName_ShouldReturnPlaceholderWithTypeNameFromResolver()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new(
            typeof(TestEndpointResolver),
            envelope => new TestEndpointResolver().GetTopicPartition(envelope));

        endpointResolver.RawName.ShouldStartWith("dynamic-TestEndpointResolver-");
    }

    [Theory]
    [InlineData("topic", 42)]
    [InlineData("topic", -1)]
    public void GetSerializedEndpoint_ShouldSerializeDestinationTopic(string topic, int partition)
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new(
            (IOutboundEnvelope<TestEventOne> _) =>
                new TopicPartition(topic, partition));

        string result = endpointResolver.GetSerializedEndpoint(_envelope);

        result.ShouldBe($"{topic}|{partition}");
    }

    [Theory]
    [InlineData("serialized", 42)]
    [InlineData("serialized", -1)]
    public void GetEndpoint_ShouldDeserializeEndpoint(string topic, int partition)
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new((IOutboundEnvelope<TestEventOne> _) => "topic");
        IOutboundEnvelope envelope = new OutboundEnvelope(
            null,
            [new MessageHeader(DefaultMessageHeaders.SerializedEndpoint, $"{topic}|{partition}")],
            new KafkaProducerEndpointConfiguration(),
            Substitute.For<IProducer>());

        ProducerEndpoint endpoint = endpointResolver.GetEndpoint(envelope);

        KafkaProducerEndpoint kafkaEndpoint = endpoint.ShouldBeOfType<KafkaProducerEndpoint>();
        kafkaEndpoint.TopicPartition.ShouldBe(new TopicPartition(topic, partition));
    }

    private sealed class TestEndpointResolver : IKafkaProducerEndpointResolver<TestEventOne>
    {
        public TopicPartition GetTopicPartition(IOutboundEnvelope<TestEventOne> envelope) => new("topic", 42);
    }
}
