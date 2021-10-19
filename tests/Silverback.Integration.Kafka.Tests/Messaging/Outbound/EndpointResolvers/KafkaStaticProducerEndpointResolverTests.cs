// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.Outbound.EndpointResolvers;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Outbound.EndpointResolvers;

public class KafkaStaticProducerEndpointResolverTests
{
    [Fact]
    public void GetEndpoint_WithoutPartition_StaticTopicPartitionReturned()
    {
        KafkaStaticProducerEndpointResolver endpointResolver = new("topic");

        ProducerEndpoint endpoint = endpointResolver.GetEndpoint(null, new KafkaProducerConfiguration(), null!);

        endpoint.Should().NotBeNull();
        endpoint.Should().BeOfType<KafkaProducerEndpoint>();
        endpoint.As<KafkaProducerEndpoint>().TopicPartition.Topic.Should().Be("topic");
        endpoint.As<KafkaProducerEndpoint>().TopicPartition.Partition.Should().Be(Partition.Any);
    }

    [Fact]
    public void GetEndpoint_SpecificPartition_StaticTopicPartitionReturned()
    {
        KafkaStaticProducerEndpointResolver endpointResolver = new("topic", 42);

        ProducerEndpoint endpoint = endpointResolver.GetEndpoint(null, new KafkaProducerConfiguration(), null!);

        endpoint.Should().NotBeNull();
        endpoint.Should().BeOfType<KafkaProducerEndpoint>();
        endpoint.As<KafkaProducerEndpoint>().TopicPartition.Topic.Should().Be("topic");
        endpoint.As<KafkaProducerEndpoint>().TopicPartition.Partition.Should().Be(new Partition(42));
    }

    [Fact]
    public void RawName_WithoutPartition_TopicNameReturned()
    {
        KafkaStaticProducerEndpointResolver endpointResolver = new("topic");

        endpointResolver.RawName.Should().Be("topic");
    }

    [Fact]
    public void RawName_SpecificPartition_TopicNameAndPartitionReturned()
    {
        KafkaStaticProducerEndpointResolver endpointResolver = new("topic", 42);

        endpointResolver.RawName.Should().Be("topic[42]");
    }

    [Fact]
    public void Constructor_ValidTopic_NoExceptionThrown()
    {
        Action act = () => _ = new KafkaStaticProducerEndpointResolver("topic");

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_ValidTopicAndPartition_NoExceptionThrown()
    {
        Action act = () => _ = new KafkaStaticProducerEndpointResolver("topic", 42);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Constructor_EmptyTopic_ExceptionThrown(string? topic)
    {
        Action act = () => _ = new KafkaStaticProducerEndpointResolver(topic!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_InvalidPartition_ExceptionThrown()
    {
        Action act = () => _ = _ = new KafkaStaticProducerEndpointResolver("topic", -42);

        act.Should().ThrowExactly<ArgumentException>();
    }
}
