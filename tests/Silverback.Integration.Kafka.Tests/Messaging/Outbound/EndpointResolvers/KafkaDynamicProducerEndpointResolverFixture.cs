// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Text;
using System.Threading.Tasks;
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
        KafkaDynamicProducerEndpointResolver endpointResolver = new("topic", _ => 42);

        KafkaProducerEndpoint endpoint = endpointResolver.GetEndpoint(null, new KafkaProducerEndpointConfiguration(), Substitute.For<IServiceProvider>());

        endpoint.Should().NotBeNull();
        endpoint.TopicPartition.Topic.Should().Be("topic");
        endpoint.TopicPartition.Partition.Should().Be(new Partition(42));
    }

    [Fact]
    public void GetEndpoint_ShouldReturnTopicFromTopicNameFunction()
    {
        KafkaDynamicProducerEndpointResolver endpointResolver = new(_ => "topic");

        KafkaProducerEndpoint endpoint = endpointResolver.GetEndpoint(null, new KafkaProducerEndpointConfiguration(), Substitute.For<IServiceProvider>());

        endpoint.Should().NotBeNull();
        endpoint.TopicPartition.Topic.Should().Be("topic");
        endpoint.TopicPartition.Partition.Should().Be(Partition.Any);
    }

    [Fact]
    public void GetEndpoint_ShouldReturnTopicAndPartitionFromTopicNameFunctionAndPartitionFunction()
    {
        KafkaDynamicProducerEndpointResolver endpointResolver = new(_ => "topic", _ => 42);

        KafkaProducerEndpoint endpoint = endpointResolver.GetEndpoint(null, new KafkaProducerEndpointConfiguration(), Substitute.For<IServiceProvider>());

        endpoint.Should().NotBeNull();
        endpoint.TopicPartition.Topic.Should().Be("topic");
        endpoint.TopicPartition.Partition.Should().Be(new Partition(42));
    }

    [Fact]
    public void GetEndpoint_ShouldReturnTopicAndPartitionFromTopicPartitionFunction()
    {
        KafkaDynamicProducerEndpointResolver endpointResolver = new(_ => new TopicPartition("topic", 42));

        KafkaProducerEndpoint endpoint = endpointResolver.GetEndpoint(null, new KafkaProducerEndpointConfiguration(), Substitute.For<IServiceProvider>());

        endpoint.Should().NotBeNull();
        endpoint.TopicPartition.Topic.Should().Be("topic");
        endpoint.TopicPartition.Partition.Should().Be(new Partition(42));
    }

    [Fact]
    public void GetEndpoint_ShouldReturnTopicAndPartitionFromTopicNameFormatAndPartitionFunction()
    {
        KafkaDynamicProducerEndpointResolver endpointResolver = new("topic-{0}", _ => new[] { "123" }, _ => 42);

        KafkaProducerEndpoint endpoint = endpointResolver.GetEndpoint(null, new KafkaProducerEndpointConfiguration(), Substitute.For<IServiceProvider>());

        endpoint.Should().NotBeNull();
        endpoint.TopicPartition.Topic.Should().Be("topic-123");
        endpoint.TopicPartition.Partition.Should().Be(new Partition(42));
    }

    [Fact]
    public void GetEndpoint_ShouldReturnTopicAndPartitionFromResolver()
    {
        KafkaDynamicProducerEndpointResolver endpointResolver = new(
            typeof(TestEndpointResolver),
            (message, serviceProvider) => serviceProvider.GetRequiredService<TestEndpointResolver>().GetTopicPartition((TestEventOne?)message));

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
        KafkaDynamicProducerEndpointResolver endpointResolver = new("topic", _ => 42);

        endpointResolver.RawName.Should().StartWith("topic");
    }

    [Fact]
    public void RawName_ShouldReturnPlaceholderFromTopicNameFunction()
    {
        KafkaDynamicProducerEndpointResolver endpointResolver = new(_ => "topic");

        endpointResolver.RawName.Should().StartWith("dynamic-");
    }

    [Fact]
    public void RawName_ShouldReturnPlaceholderFromTopicNameFunctionAndPartitionFunction()
    {
        KafkaDynamicProducerEndpointResolver endpointResolver = new(_ => "topic", _ => 42);

        endpointResolver.RawName.Should().StartWith("dynamic-");
    }

    [Fact]
    public void RawName_ShouldReturnPlaceholderFromTopicPartitionFunction()
    {
        KafkaDynamicProducerEndpointResolver endpointResolver = new(_ => new TopicPartition("topic", 42));

        endpointResolver.RawName.Should().StartWith("dynamic-");
    }

    [Fact]
    public void RawName_ShouldReturnFormatStringFromTopicFormat()
    {
        KafkaDynamicProducerEndpointResolver endpointResolver = new("topic-{0}", _ => new[] { "123" });

        endpointResolver.RawName.Should().StartWith("topic-{0}");
    }

    [Fact]
    public void RawName_ShouldReturnPlaceholderWithTypeNameFromResolver()
    {
        KafkaDynamicProducerEndpointResolver endpointResolver = new(
            typeof(TestEndpointResolver),
            (message, serviceProvider) => serviceProvider.GetRequiredService<TestEndpointResolver>().GetTopicPartition((TestEventOne?)message));

        endpointResolver.RawName.Should().StartWith("dynamic-TestEndpointResolver-");
    }

    [Theory]
    [InlineData("topic", 42)]
    [InlineData("topic", -1)]
    public async Task SerializeAsync_ShouldSerializeTargetTopicAndPartition(string topic, int partition)
    {
        KafkaDynamicProducerEndpointResolver endpointResolver = new(_ => "abc");
        KafkaProducerEndpoint endpoint = new(topic, partition, new KafkaProducerEndpointConfiguration());

        byte[] result = await endpointResolver.SerializeAsync(endpoint);

        Encoding.UTF8.GetString(result).Should().Be($"{topic}|{partition}");
    }

    [Theory]
    [InlineData("topic", 42)]
    [InlineData("topic", -1)]
    public async Task DeserializeAsync_ShouldDeserializeEndpoint(string topic, int partition)
    {
        KafkaDynamicProducerEndpointResolver endpointResolver = new(_ => "abc");
        byte[] serialized = Encoding.UTF8.GetBytes($"{topic}|{partition}");

        KafkaProducerEndpoint result = await endpointResolver.DeserializeAsync(serialized, new KafkaProducerEndpointConfiguration());
        result.Should().NotBeNull();
        result.TopicPartition.Should().BeEquivalentTo(new TopicPartition(topic, partition));
    }

    private sealed class TestEndpointResolver : IKafkaProducerEndpointResolver<TestEventOne>
    {
        public TopicPartition GetTopicPartition(TestEventOne? message) => new("topic", 42);
    }
}
