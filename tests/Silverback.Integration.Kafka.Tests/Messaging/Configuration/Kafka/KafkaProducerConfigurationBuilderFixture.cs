// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Confluent.Kafka;
using FluentAssertions;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka;

public class KafkaProducerConfigurationBuilderFixture
{
    [Fact]
    public void Build_ShouldThrow_WhenConfigurationIsNotValid()
    {
        KafkaProducerConfigurationBuilder builder = new();

        Action act = () => builder.Build();

        act.Should().Throw<SilverbackConfigurationException>();
    }

    [Fact]
    public void Build_ShouldReturnNewConfiguration_WhenReused()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.WithBootstrapServers("one");
        KafkaProducerConfiguration configuration1 = builder.Build();

        builder.WithBootstrapServers("two");
        KafkaProducerConfiguration configuration2 = builder.Build();

        configuration1.BootstrapServers.Should().Be("one");
        configuration2.BootstrapServers.Should().Be("two");
    }

    [Fact]
    public void Produce_ShouldAddEndpoints()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1"))
            .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo("topic2"));

        KafkaProducerConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.Endpoints.Should().HaveCount(2);
        KafkaProducerEndpointConfiguration endpoint1 = configuration.Endpoints.First();
        endpoint1.Endpoint.As<KafkaStaticProducerEndpointResolver>().TopicPartition
            .Should().BeEquivalentTo(new TopicPartition("topic1", Partition.Any));
        endpoint1.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
        KafkaProducerEndpointConfiguration endpoint2 = configuration.Endpoints.Skip(1).First();
        endpoint2.Endpoint.As<KafkaStaticProducerEndpointResolver>().TopicPartition
            .Should().BeEquivalentTo(new TopicPartition("topic2", Partition.Any));
        endpoint2.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventTwo>>();
    }

    [Fact]
    public void Produce_ShouldAddEndpointWithGenericMessageType_WhenNoTypeIsSpecified()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder.Produce(endpoint => endpoint.ProduceTo("topic1"));

        KafkaProducerConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.Endpoints.Should().HaveCount(1);
        KafkaProducerEndpointConfiguration endpoint = configuration.Endpoints.Single();
        endpoint.MessageType.Should().Be<object>();
        endpoint.Endpoint.As<KafkaStaticProducerEndpointResolver>().TopicPartition
            .Should().BeEquivalentTo(new TopicPartition("topic1", Partition.Any));
        endpoint.Serializer.Should().BeOfType<JsonMessageSerializer<object>>();
    }

    [Fact]
    public void Produce_ShouldIgnoreEndpointsWithSameId()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Produce<TestEventOne>("id1", endpoint => endpoint.ProduceTo("topic1"))
            .Produce<TestEventTwo>("id1", endpoint => endpoint.ProduceTo("topic1"));

        KafkaProducerConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.Endpoints.Should().HaveCount(1);
        KafkaProducerEndpointConfiguration endpoint1 = configuration.Endpoints.First();
        endpoint1.Endpoint.As<KafkaStaticProducerEndpointResolver>().TopicPartition
            .Should().BeEquivalentTo(new TopicPartition("topic1", Partition.Any));
        endpoint1.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
    }

    [Fact]
    public void Produce_ShouldIgnoreEndpointsWithSameTopicNameAndMessageTypeAndNoId()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1").EnableChunking(42))
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1").EnableChunking(1000));

        KafkaProducerConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.Endpoints.Should().HaveCount(1);
        KafkaProducerEndpointConfiguration endpoint1 = configuration.Endpoints.First();
        endpoint1.Endpoint.As<KafkaStaticProducerEndpointResolver>().TopicPartition
            .Should().BeEquivalentTo(new TopicPartition("topic1", Partition.Any));
        endpoint1.Serializer.Should().BeOfType<JsonMessageSerializer<TestEventOne>>();
        endpoint1.Chunk!.Size.Should().Be(42);
    }

    [Fact]
    public void Produce_ShouldAddEndpointsWithSameTopicNameAndDifferentMessageType()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1"))
            .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo("topic1"));

        KafkaProducerConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.Endpoints.Should().HaveCount(2);
    }

    [Fact]
    public void Produce_ShouldAddEndpointsWithSameTopicNameAndDifferentId()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Produce<TestEventOne>("id1", endpoint => endpoint.ProduceTo("topic1"))
            .Produce<TestEventTwo>("id2", endpoint => endpoint.ProduceTo("topic1"));

        KafkaProducerConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.Endpoints.Should().HaveCount(2);
    }

    [Fact]
    public void ThrowIfNotAcknowledged_ShouldSetThrowIfNotAcknowledged()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.ThrowIfNotAcknowledged();

        KafkaProducerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.ThrowIfNotAcknowledged.Should().BeTrue();
    }

    [Fact]
    public void IgnoreIfNotAcknowledged_ShouldSetThrowIfNotAcknowledged()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.IgnoreIfNotAcknowledged();

        KafkaProducerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.ThrowIfNotAcknowledged.Should().BeFalse();
    }

    [Fact]
    public void DisposeOnException_ShouldSetDisposeOnException()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.DisposeOnException();

        KafkaProducerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.DisposeOnException.Should().BeTrue();
    }

    [Fact]
    public void DisableDisposeOnException_ShouldSetDisposeOnException()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.DisableDisposeOnException();

        KafkaProducerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.DisposeOnException.Should().BeFalse();
    }

    [Fact]
    public void WithFlushTimeout_ShouldSetFlushTimeout()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.WithFlushTimeout(TimeSpan.FromHours(42));

        KafkaProducerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.FlushTimeout.Should().Be(TimeSpan.FromHours(42));
    }

    [Fact]
    public void EnableDeliveryReports_ShouldSetEnableDeliveryReports()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.EnableDeliveryReports();

        KafkaProducerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.EnableDeliveryReports.Should().BeTrue();
    }

    [Fact]
    public void DisableDeliveryReports_ShouldSetEnableDeliveryReports()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.DisableDeliveryReports();
        builder.IgnoreIfNotAcknowledged();

        KafkaProducerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.EnableDeliveryReports.Should().BeFalse();
    }

    [Fact]
    public void EnableIdempotence_ShouldSetEnableIdempotence()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.EnableIdempotence();

        KafkaProducerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.EnableIdempotence.Should().BeTrue();
    }

    [Fact]
    public void DisableIdempotence_ShouldSetEnableIdempotence()
    {
        KafkaProducerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.DisableIdempotence();

        KafkaProducerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.EnableIdempotence.Should().BeFalse();
    }

    private static KafkaProducerConfigurationBuilder GetBuilderWithValidConfigurationAndEndpoint() =>
        GetBuilderWithValidConfiguration().Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic"));

    private static KafkaProducerConfigurationBuilder GetBuilderWithValidConfiguration() =>
        new KafkaProducerConfigurationBuilder().WithBootstrapServers("PLAINTEXT://test");
}
