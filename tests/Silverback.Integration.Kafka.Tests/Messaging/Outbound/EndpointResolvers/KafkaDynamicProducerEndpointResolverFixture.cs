// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Silverback.Messaging;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Outbound.EndpointResolvers;

public class KafkaDynamicProducerEndpointResolverFixture
{
    [Fact]
    public void GetEndpoint_ShouldReturnTopicAndPartitionFromTopicNameAndPartitionFunction()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new("topic", _ => 42);

        KafkaProducerEndpoint endpoint = endpointResolver.GetEndpoint(null, new KafkaProducerEndpointConfiguration(), Substitute.For<IServiceProvider>());

        endpoint.Should().NotBeNull();
        endpoint.TopicPartition.Topic.Should().Be("topic");
        endpoint.TopicPartition.Partition.Should().Be(new Partition(42));
    }

    [Fact]
    public void GetEndpoint_ShouldReturnTopicFromTopicNameFunction()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new(_ => "topic");

        KafkaProducerEndpoint endpoint = endpointResolver.GetEndpoint(null, new KafkaProducerEndpointConfiguration(), Substitute.For<IServiceProvider>());

        endpoint.Should().NotBeNull();
        endpoint.TopicPartition.Topic.Should().Be("topic");
        endpoint.TopicPartition.Partition.Should().Be(Partition.Any);
    }

    [Fact]
    public void GetEndpoint_ShouldReturnTopicAndPartitionFromTopicNameFunctionAndPartitionFunction()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new(_ => "topic", _ => 42);

        KafkaProducerEndpoint endpoint = endpointResolver.GetEndpoint(null, new KafkaProducerEndpointConfiguration(), Substitute.For<IServiceProvider>());

        endpoint.Should().NotBeNull();
        endpoint.TopicPartition.Topic.Should().Be("topic");
        endpoint.TopicPartition.Partition.Should().Be(new Partition(42));
    }

    [Fact]
    public void GetEndpoint_ShouldReturnTopicAndPartitionFromTopicPartitionFunction()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new(_ => new TopicPartition("topic", 42));

        KafkaProducerEndpoint endpoint = endpointResolver.GetEndpoint(null, new KafkaProducerEndpointConfiguration(), Substitute.For<IServiceProvider>());

        endpoint.Should().NotBeNull();
        endpoint.TopicPartition.Topic.Should().Be("topic");
        endpoint.TopicPartition.Partition.Should().Be(new Partition(42));
    }

    [Fact]
    public void GetEndpoint_ShouldReturnTopicAndPartitionFromTopicNameFormatAndPartitionFunction()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new("topic-{0}", _ => ["123"], _ => 42);

        KafkaProducerEndpoint endpoint = endpointResolver.GetEndpoint(null, new KafkaProducerEndpointConfiguration(), Substitute.For<IServiceProvider>());

        endpoint.Should().NotBeNull();
        endpoint.TopicPartition.Topic.Should().Be("topic-123");
        endpoint.TopicPartition.Partition.Should().Be(new Partition(42));
    }

    [Fact]
    public void GetEndpoint_ShouldReturnTopicAndPartitionFromResolver()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new(
            typeof(TestEndpointResolver),
            (message, serviceProvider) => serviceProvider.GetRequiredService<TestEndpointResolver>().GetTopicPartition(message));

        IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(TestEndpointResolver)).Returns(new TestEndpointResolver());

        KafkaProducerEndpoint endpoint = endpointResolver.GetEndpoint(new TestEventOne(), new KafkaProducerEndpointConfiguration(), serviceProvider);

        endpoint.Should().NotBeNull();
        endpoint.TopicPartition.Topic.Should().Be("topic");
        endpoint.TopicPartition.Partition.Should().Be(new Partition(42));
    }

    [Fact]
    public void RawName_ShouldReturnTopicName()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new("topic", _ => 42);

        endpointResolver.RawName.Should().StartWith("topic");
    }

    [Fact]
    public void RawName_ShouldReturnPlaceholderFromTopicNameFunction()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new(_ => "topic");

        endpointResolver.RawName.Should().StartWith("dynamic-");
    }

    [Fact]
    public void RawName_ShouldReturnPlaceholderFromTopicNameFunctionAndPartitionFunction()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new(_ => "topic", _ => 42);

        endpointResolver.RawName.Should().StartWith("dynamic-");
    }

    [Fact]
    public void RawName_ShouldReturnPlaceholderFromTopicPartitionFunction()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new(_ => new TopicPartition("topic", 42));

        endpointResolver.RawName.Should().StartWith("dynamic-");
    }

    [Fact]
    public void RawName_ShouldReturnFormatStringFromTopicFormat()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new("topic-{0}", _ => ["123"]);

        endpointResolver.RawName.Should().StartWith("topic-{0}");
    }

    [Fact]
    public void RawName_ShouldReturnPlaceholderWithTypeNameFromResolver()
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new(
            typeof(TestEndpointResolver),
            (message, serviceProvider) => serviceProvider.GetRequiredService<TestEndpointResolver>().GetTopicPartition(message));

        endpointResolver.RawName.Should().StartWith("dynamic-TestEndpointResolver-");
    }

    [Theory]
    [InlineData("topic", 42)]
    [InlineData("topic", -1)]
    public void Serialize_ShouldSerializeTargetTopicAndPartition(string topic, int partition)
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new(_ => "abc");
        KafkaProducerEndpoint endpoint = new(topic, partition, new KafkaProducerEndpointConfiguration());

        string result = endpointResolver.Serialize(endpoint);

        result.Should().Be($"{topic}|{partition}");
    }

    [Theory]
    [InlineData("topic", 42)]
    [InlineData("topic", -1)]
    public void Deserialize_ShouldDeserializeEndpoint(string topic, int partition)
    {
        KafkaDynamicProducerEndpointResolver<TestEventOne> endpointResolver = new(_ => "abc");
        string serialized = $"{topic}|{partition}";

        KafkaProducerEndpoint result = endpointResolver.Deserialize(serialized, new KafkaProducerEndpointConfiguration());
        result.Should().NotBeNull();
        result.TopicPartition.Should().BeEquivalentTo(new TopicPartition(topic, partition));
    }

    private sealed class TestEndpointResolver : IKafkaProducerEndpointResolver<TestEventOne>
    {
        public TopicPartition GetTopicPartition(TestEventOne? message) => new("topic", 42);
    }
}
