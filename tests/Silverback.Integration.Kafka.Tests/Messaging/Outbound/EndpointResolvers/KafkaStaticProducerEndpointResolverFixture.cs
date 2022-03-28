// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using FluentAssertions;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Producing.EndpointResolvers;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Outbound.EndpointResolvers;

public class KafkaStaticProducerEndpointResolverFixture
{
    [Fact]
    public void GetEndpoint_ShouldReturnTopicAndPartitionFromTopicName()
    {
        KafkaStaticProducerEndpointResolver endpointResolver = new("topic");

        ProducerEndpoint endpoint = endpointResolver.GetEndpoint(null, new KafkaProducerEndpointConfiguration(), null!);

        endpoint.Should().NotBeNull();
        endpoint.Should().BeOfType<KafkaProducerEndpoint>();
        endpoint.As<KafkaProducerEndpoint>().TopicPartition.Topic.Should().Be("topic");
        endpoint.As<KafkaProducerEndpoint>().TopicPartition.Partition.Should().Be(Partition.Any);
    }

    [Fact]
    public void GetEndpoint_ShouldReturnTopicAndPartitionFromTopicAndPartition()
    {
        KafkaStaticProducerEndpointResolver endpointResolver = new("topic", 42);

        ProducerEndpoint endpoint = endpointResolver.GetEndpoint(null, new KafkaProducerEndpointConfiguration(), null!);

        endpoint.Should().NotBeNull();
        endpoint.Should().BeOfType<KafkaProducerEndpoint>();
        endpoint.As<KafkaProducerEndpoint>().TopicPartition.Topic.Should().Be("topic");
        endpoint.As<KafkaProducerEndpoint>().TopicPartition.Partition.Should().Be(new Partition(42));
    }

    [Fact]
    public void RawName_ShouldReturnTopicName_WhenPartitionIsNotSpecified()
    {
        KafkaStaticProducerEndpointResolver endpointResolver = new("topic");

        endpointResolver.RawName.Should().Be("topic");
    }

    [Fact]
    public void RawName_ShouldReturnTopicAndPartition()
    {
        KafkaStaticProducerEndpointResolver endpointResolver = new("topic", 42);

        endpointResolver.RawName.Should().Be("topic[42]");
    }

    [Fact]
    public void Constructor_ShouldNotThrow_WhenTopicNameIsValid()
    {
        Action act = () => _ = new KafkaStaticProducerEndpointResolver("topic");

        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_ShouldNotThrow_WhenTopicNameAndPartitionAreValid()
    {
        Action act = () => _ = new KafkaStaticProducerEndpointResolver("topic", 42);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Constructor_ShouldThrow_WhenTopicNameIsNotValid(string? topic)
    {
        Action act = () => _ = new KafkaStaticProducerEndpointResolver(topic!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenPartitionIsNotValid()
    {
        Action act = () => _ = _ = new KafkaStaticProducerEndpointResolver("topic", -42);

        act.Should().ThrowExactly<ArgumentException>();
    }
}
