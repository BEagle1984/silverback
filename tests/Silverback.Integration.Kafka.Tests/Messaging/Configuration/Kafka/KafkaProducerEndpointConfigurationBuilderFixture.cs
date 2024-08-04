// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka;

public class KafkaProducerEndpointConfigurationBuilderFixture
{
    [Fact]
    public void Build_ShouldThrow_WhenConfigurationIsNotValid()
    {
        KafkaProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        Action act = () => builder.Build();

        act.Should().ThrowExactly<BrokerConfigurationException>();
    }

    [Fact]
    public void ProduceTo_ShouldSetEndpointFromTopicName()
    {
        KafkaProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo("some-topic");

        KafkaProducerEndpointConfiguration configuration = builder.Build();
        configuration.Endpoint.Should().BeOfType<KafkaStaticProducerEndpointResolver>();
        configuration.RawName.Should().Be("some-topic");
        KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)configuration.Endpoint.GetEndpoint(
            null,
            configuration,
            Substitute.For<IServiceProvider>());
        endpoint.TopicPartition.Should().Be(new TopicPartition("some-topic", Partition.Any));
    }

    [Fact]
    public void ProduceTo_ShouldSetEndpointFromTopicNameAndPartition()
    {
        KafkaProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo("some-topic", 42);

        KafkaProducerEndpointConfiguration configuration = builder.Build();
        configuration.Endpoint.Should().BeOfType<KafkaStaticProducerEndpointResolver>();
        configuration.RawName.Should().Be("some-topic[42]");
        KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)configuration.Endpoint.GetEndpoint(
            null,
            configuration,
            Substitute.For<IServiceProvider>());
        endpoint.TopicPartition.Should().Be(new TopicPartition("some-topic", 42));
    }

    [Fact]
    public void ProduceTo_ShouldSetEndpointFromTopicPartition()
    {
        KafkaProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo(new TopicPartition("some-topic", 42));

        KafkaProducerEndpointConfiguration configuration = builder.Build();
        configuration.Endpoint.Should().BeOfType<KafkaStaticProducerEndpointResolver>();
        configuration.RawName.Should().Be("some-topic[42]");
        KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)configuration.Endpoint.GetEndpoint(
            null,
            configuration,
            Substitute.For<IServiceProvider>());
        endpoint.TopicPartition.Should().Be(new TopicPartition("some-topic", 42));
    }

    [Fact]
    public void ProduceTo_ShouldSetEndpointFromTopicNameAndPartitionFunction()
    {
        KafkaProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo("some-topic", _ => 42);

        KafkaProducerEndpointConfiguration configuration = builder.Build();
        configuration.Endpoint.Should().BeOfType<KafkaDynamicProducerEndpointResolver<TestEventOne>>();
        configuration.RawName.Should().Be("some-topic");
        KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)configuration.Endpoint.GetEndpoint(
            null,
            configuration,
            Substitute.For<IServiceProvider>());
        endpoint.TopicPartition.Should().Be(new TopicPartition("some-topic", 42));
    }

    [Fact]
    public void ProduceTo_ShouldSetEndpointFromTopicNameFunction()
    {
        KafkaProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo(_ => "some-topic");

        KafkaProducerEndpointConfiguration configuration = builder.Build();
        configuration.Endpoint.Should().BeOfType<KafkaDynamicProducerEndpointResolver<TestEventOne>>();
        configuration.RawName.Should().StartWith("dynamic-");
        KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)configuration.Endpoint.GetEndpoint(
            null,
            configuration,
            Substitute.For<IServiceProvider>());
        endpoint.TopicPartition.Should().Be(new TopicPartition("some-topic", Partition.Any));
    }

    [Fact]
    public void ProduceTo_ShouldSetEndpointFromTopicNameFunctionAndPartitionFunction()
    {
        KafkaProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo(_ => "some-topic", _ => 42);

        KafkaProducerEndpointConfiguration configuration = builder.Build();
        configuration.Endpoint.Should().BeOfType<KafkaDynamicProducerEndpointResolver<TestEventOne>>();
        configuration.RawName.Should().StartWith("dynamic-");
        KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)configuration.Endpoint.GetEndpoint(
            null,
            configuration,
            Substitute.For<IServiceProvider>());
        endpoint.TopicPartition.Should().Be(new TopicPartition("some-topic", new Partition(42)));
    }

    [Fact]
    public void ProduceTo_ShouldSetEndpointFromTopicPartitionFunction()
    {
        KafkaProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo(_ => new TopicPartition("some-topic", 42));

        KafkaProducerEndpointConfiguration configuration = builder.Build();
        configuration.Endpoint.Should().BeOfType<KafkaDynamicProducerEndpointResolver<TestEventOne>>();
        configuration.RawName.Should().StartWith("dynamic-");
        KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)configuration.Endpoint.GetEndpoint(
            null,
            configuration,
            Substitute.For<IServiceProvider>());
        endpoint.TopicPartition.Should().Be(new TopicPartition("some-topic", new Partition(42)));
    }

    [Fact]
    public void ProduceTo_ShouldSetEndpointFromTopicNameFormat()
    {
        KafkaProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo("some-topic-{0}", _ => ["123"], _ => 42);

        KafkaProducerEndpointConfiguration configuration = builder.Build();
        configuration.Endpoint.Should().BeOfType<KafkaDynamicProducerEndpointResolver<TestEventOne>>();
        configuration.RawName.Should().Be("some-topic-{0}");
        KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)configuration.Endpoint.GetEndpoint(
            null,
            configuration,
            Substitute.For<IServiceProvider>());
        endpoint.TopicPartition.Should().Be(new TopicPartition("some-topic-123", new Partition(42)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ProduceTo_ShouldThrow_WhenTopicNameIsNotValid(string? topicName)
    {
        KafkaProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        Action act = () => builder.ProduceTo(topicName!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ProduceTo_ShouldThrow_WhenPartitionIndexIsNotValid()
    {
        KafkaProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        Action act = () => builder.ProduceTo("test", -42);

        act.Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void UseEndpointResolver_ShouldSetEndpoint()
    {
        KafkaProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.UseEndpointResolver<TestTypedEndpointResolver>();

        KafkaProducerEndpointConfiguration configuration = builder.Build();
        configuration.Endpoint.Should().BeOfType<KafkaDynamicProducerEndpointResolver<TestEventOne>>();
        configuration.RawName.Should().StartWith("dynamic-TestTypedEndpointResolver-");
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Class used via DI")]
    private sealed class TestTypedEndpointResolver : IKafkaProducerEndpointResolver<TestEventOne>
    {
        public TopicPartition GetTopicPartition(TestEventOne? message) => new("some-topic", 42);
    }
}
