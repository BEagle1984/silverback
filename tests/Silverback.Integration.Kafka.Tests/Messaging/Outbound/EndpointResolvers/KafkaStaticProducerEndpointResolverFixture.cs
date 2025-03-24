// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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

public class KafkaStaticProducerEndpointResolverFixture
{
    private readonly IOutboundEnvelope<TestEventOne> _envelope = new OutboundEnvelope<TestEventOne>(
        new TestEventOne(),
        null,
        new KafkaProducerEndpointConfiguration(),
        Substitute.For<IProducer>());

    [Fact]
    public void GetEndpoint_ShouldReturnTopicAndPartitionFromTopicName()
    {
        KafkaStaticProducerEndpointResolver endpointResolver = new("topic");

        ProducerEndpoint endpoint = endpointResolver.GetEndpoint(_envelope);

        endpoint.ShouldNotBeNull();
        KafkaProducerEndpoint kafkaEndpoint = endpoint.ShouldBeOfType<KafkaProducerEndpoint>();
        kafkaEndpoint.TopicPartition.Topic.ShouldBe("topic");
        kafkaEndpoint.TopicPartition.Partition.ShouldBe(Partition.Any);
    }

    [Fact]
    public void GetEndpoint_ShouldReturnTopicAndPartitionFromTopicAndPartition()
    {
        KafkaStaticProducerEndpointResolver endpointResolver = new("topic", 42);

        ProducerEndpoint endpoint = endpointResolver.GetEndpoint(_envelope);

        endpoint.ShouldNotBeNull();
        KafkaProducerEndpoint kafkaEndpoint = endpoint.ShouldBeOfType<KafkaProducerEndpoint>();
        kafkaEndpoint.TopicPartition.Topic.ShouldBe("topic");
        kafkaEndpoint.TopicPartition.Partition.ShouldBe(new Partition(42));
    }

    [Fact]
    public void RawName_ShouldReturnTopicName_WhenPartitionIsNotSpecified()
    {
        KafkaStaticProducerEndpointResolver endpointResolver = new("topic");

        endpointResolver.RawName.ShouldBe("topic");
    }

    [Fact]
    public void RawName_ShouldReturnTopicAndPartition()
    {
        KafkaStaticProducerEndpointResolver endpointResolver = new("topic", 42);

        endpointResolver.RawName.ShouldBe("topic[42]");
    }

    [Fact]
    public void Constructor_ShouldNotThrow_WhenTopicNameIsValid()
    {
        Action act = () => _ = new KafkaStaticProducerEndpointResolver("topic");

        act.ShouldNotThrow();
    }

    [Fact]
    public void Constructor_ShouldNotThrow_WhenTopicNameAndPartitionAreValid()
    {
        Action act = () => _ = new KafkaStaticProducerEndpointResolver("topic", 42);

        act.ShouldNotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Constructor_ShouldThrow_WhenTopicNameIsNotValid(string? topic)
    {
        Action act = () => _ = new KafkaStaticProducerEndpointResolver(topic!);

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenPartitionIsNotValid()
    {
        Action act = () => _ = _ = new KafkaStaticProducerEndpointResolver("topic", -42);

        act.ShouldThrow<ArgumentException>();
    }
}
