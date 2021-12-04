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
using Silverback.Messaging.Outbound.EndpointResolvers;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Outbound.EndpointResolvers;

public class KafkaDynamicProducerEndpointResolverTests
{
    [Fact]
    public void GetEndpoint_TopicNameAndPartitionFunction_TopicAndPartitionReturned()
    {
        KafkaDynamicProducerEndpointResolver endpointResolver = new("topic", _ => 42);

        KafkaProducerEndpoint endpoint = endpointResolver.GetEndpoint(null, new KafkaProducerConfiguration(), Substitute.For<IServiceProvider>());

        endpoint.Should().NotBeNull();
        endpoint.TopicPartition.Topic.Should().Be("topic");
        endpoint.TopicPartition.Partition.Should().Be(new Partition(42));
    }

    [Fact]
    public void GetEndpoint_TopicNameFunction_TopicReturned()
    {
        KafkaDynamicProducerEndpointResolver endpointResolver = new(_ => "topic");

        KafkaProducerEndpoint endpoint = endpointResolver.GetEndpoint(null, new KafkaProducerConfiguration(), Substitute.For<IServiceProvider>());

        endpoint.Should().NotBeNull();
        endpoint.TopicPartition.Topic.Should().Be("topic");
        endpoint.TopicPartition.Partition.Should().Be(Partition.Any);
    }

    [Fact]
    public void GetEndpoint_TopicNameFunctionAndPartitionFunction_TopicAndPartitionReturned()
    {
        KafkaDynamicProducerEndpointResolver endpointResolver = new(_ => "topic", _ => 42);

        KafkaProducerEndpoint endpoint = endpointResolver.GetEndpoint(null, new KafkaProducerConfiguration(), Substitute.For<IServiceProvider>());

        endpoint.Should().NotBeNull();
        endpoint.TopicPartition.Topic.Should().Be("topic");
        endpoint.TopicPartition.Partition.Should().Be(new Partition(42));
    }

    [Fact]
    public void GetEndpoint_TopicPartitionFunction_TopicAndPartitionReturned()
    {
        KafkaDynamicProducerEndpointResolver endpointResolver = new(_ => new TopicPartition("topic", 42));

        KafkaProducerEndpoint endpoint = endpointResolver.GetEndpoint(null, new KafkaProducerConfiguration(), Substitute.For<IServiceProvider>());

        endpoint.Should().NotBeNull();
        endpoint.TopicPartition.Topic.Should().Be("topic");
        endpoint.TopicPartition.Partition.Should().Be(new Partition(42));
    }

    [Fact]
    public void GetEndpoint_TopicNameFormatAndPartitionFunction_TopicAndPartitionReturned()
    {
        KafkaDynamicProducerEndpointResolver endpointResolver = new("topic-{0}", _ => new[] { "123" }, _ => 42);

        KafkaProducerEndpoint endpoint = endpointResolver.GetEndpoint(null, new KafkaProducerConfiguration(), Substitute.For<IServiceProvider>());

        endpoint.Should().NotBeNull();
        endpoint.TopicPartition.Topic.Should().Be("topic-123");
        endpoint.TopicPartition.Partition.Should().Be(new Partition(42));
    }

    [Fact]
    public void GetEndpoint_Resolver_TopicAndPartitionReturned()
    {
        KafkaDynamicProducerEndpointResolver endpointResolver = new(
            typeof(TestEndpointResolver),
            (message, serviceProvider) => serviceProvider.GetRequiredService<TestEndpointResolver>().GetTopicPartition((TestEventOne?)message));

        IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(TestEndpointResolver)).Returns(new TestEndpointResolver());

        KafkaProducerEndpoint endpoint = endpointResolver.GetEndpoint(new TestEventOne(), new KafkaProducerConfiguration(), serviceProvider);

        endpoint.Should().NotBeNull();
        endpoint.TopicPartition.Topic.Should().Be("topic");
        endpoint.TopicPartition.Partition.Should().Be(new Partition(42));
    }

    [Fact]
    public void RawName_TopicNameAndPartitionFunction_TopicNameReturned()
    {
        KafkaDynamicProducerEndpointResolver endpointResolver = new("topic", _ => 42);

        endpointResolver.RawName.Should().StartWith("topic");
    }

    [Fact]
    public void RawName_TopicNameFunction_PlaceholderReturned()
    {
        KafkaDynamicProducerEndpointResolver endpointResolver = new(_ => "topic");

        endpointResolver.RawName.Should().StartWith("dynamic-");
    }

    [Fact]
    public void RawName_TopicNameFunctionAndPartitionFunction_PlaceholderReturned()
    {
        KafkaDynamicProducerEndpointResolver endpointResolver = new(_ => "topic", _ => 42);

        endpointResolver.RawName.Should().StartWith("dynamic-");
    }

    [Fact]
    public void RawName_TopicPartitionFunction_PlaceholderReturned()
    {
        KafkaDynamicProducerEndpointResolver endpointResolver = new(_ => new TopicPartition("topic", 42));

        endpointResolver.RawName.Should().StartWith("dynamic-");
    }

    [Fact]
    public void RawName_TopicFormat_FormatString()
    {
        KafkaDynamicProducerEndpointResolver endpointResolver = new("topic-{0}", _ => new[] { "123" });

        endpointResolver.RawName.Should().StartWith("topic-{0}");
    }

    [Fact]
    public void RawName_Resolver_PlaceholderWithTypeNameReturned()
    {
        KafkaDynamicProducerEndpointResolver endpointResolver = new(
            typeof(TestEndpointResolver),
            (message, serviceProvider) => serviceProvider.GetRequiredService<TestEndpointResolver>().GetTopicPartition((TestEventOne?)message));

        endpointResolver.RawName.Should().StartWith("dynamic-TestEndpointResolver-");
    }

    [Theory]
    [InlineData("topic", 42)]
    [InlineData("topic", -1)]
    public async Task SerializeAsync_Endpoint_SerializedTargetTopicPartitionReturned(string topic, int partition)
    {
        KafkaDynamicProducerEndpointResolver endpointResolver = new(_ => "abc");
        KafkaProducerEndpoint endpoint = new(topic, partition, new KafkaProducerConfiguration());

        byte[] result = await endpointResolver.SerializeAsync(endpoint);

        Encoding.UTF8.GetString(result).Should().Be($"{topic}|{partition}");
    }

    [Theory]
    [InlineData("topic", 42)]
    [InlineData("topic", -1)]
    public async Task DeserializeAsync_Bytes_EndpointReturned(string topic, int partition)
    {
        KafkaDynamicProducerEndpointResolver endpointResolver = new(_ => "abc");
        byte[] serialized = Encoding.UTF8.GetBytes($"{topic}|{partition}");

        KafkaProducerEndpoint result = await endpointResolver.DeserializeAsync(serialized, new KafkaProducerConfiguration());
        result.Should().NotBeNull();
        result.TopicPartition.Should().BeEquivalentTo(new TopicPartition(topic, partition));
    }

    private sealed class TestEndpointResolver : IKafkaProducerEndpointResolver<TestEventOne>
    {
        public TopicPartition GetTopicPartition(TestEventOne? message) => new("topic", 42);
    }
}
