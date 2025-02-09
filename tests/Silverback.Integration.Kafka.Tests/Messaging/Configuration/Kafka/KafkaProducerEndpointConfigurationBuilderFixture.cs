// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;
using NSubstitute;
using Shouldly;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka;

public class KafkaProducerEndpointConfigurationBuilderFixture
{
    private readonly IOutboundEnvelope<TestEventOne> _envelope = new OutboundEnvelope<TestEventOne>(
        new TestEventOne(),
        null,
        new KafkaProducerEndpointConfiguration(),
        Substitute.For<IProducer>());

    [Fact]
    public void Build_ShouldThrow_WhenConfigurationIsNotValid()
    {
        KafkaProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        Action act = () => builder.Build();

        act.ShouldThrow<BrokerConfigurationException>();
    }

    [Fact]
    public void ProduceTo_ShouldSetEndpointFromTopicName()
    {
        KafkaProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo("some-topic");

        KafkaProducerEndpointConfiguration configuration = builder.Build();
        configuration.EndpointResolver.ShouldBeOfType<KafkaStaticProducerEndpointResolver>();
        configuration.RawName.ShouldBe("some-topic");
        KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)configuration.EndpointResolver.GetEndpoint(_envelope);
        endpoint.TopicPartition.ShouldBe(new TopicPartition("some-topic", Partition.Any));
    }

    [Fact]
    public void ProduceTo_ShouldSetEndpointFromTopicNameAndPartition()
    {
        KafkaProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo("some-topic", 42);

        KafkaProducerEndpointConfiguration configuration = builder.Build();
        configuration.EndpointResolver.ShouldBeOfType<KafkaStaticProducerEndpointResolver>();
        configuration.RawName.ShouldBe("some-topic[42]");
        KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)configuration.EndpointResolver.GetEndpoint(_envelope);
        endpoint.TopicPartition.ShouldBe(new TopicPartition("some-topic", 42));
    }

    [Fact]
    public void ProduceTo_ShouldSetEndpointFromTopicPartition()
    {
        KafkaProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo(new TopicPartition("some-topic", 42));

        KafkaProducerEndpointConfiguration configuration = builder.Build();
        configuration.EndpointResolver.ShouldBeOfType<KafkaStaticProducerEndpointResolver>();
        configuration.RawName.ShouldBe("some-topic[42]");
        KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)configuration.EndpointResolver.GetEndpoint(_envelope);
        endpoint.TopicPartition.ShouldBe(new TopicPartition("some-topic", 42));
    }

    [Fact]
    public void ProduceTo_ShouldSetEndpointFromEnvelopeBasedTopicNameAndPartitionFunction()
    {
        KafkaProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo("some-topic", (IOutboundEnvelope<TestEventOne> _) => 42);

        KafkaProducerEndpointConfiguration configuration = builder.Build();
        configuration.EndpointResolver.ShouldBeOfType<KafkaDynamicProducerEndpointResolver<TestEventOne>>();
        configuration.RawName.ShouldBe("some-topic");
        KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)configuration.EndpointResolver.GetEndpoint(_envelope);
        endpoint.TopicPartition.ShouldBe(new TopicPartition("some-topic", 42));
    }

    [Fact]
    public void ProduceTo_ShouldSetEndpointFromMessageBasedTopicNameAndPartitionFunction()
    {
        KafkaProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo("some-topic", (TestEventOne? _) => 42);

        KafkaProducerEndpointConfiguration configuration = builder.Build();
        configuration.EndpointResolver.ShouldBeOfType<KafkaDynamicProducerEndpointResolver<TestEventOne>>();
        configuration.RawName.ShouldBe("some-topic");
        KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)configuration.EndpointResolver.GetEndpoint(_envelope);
        endpoint.TopicPartition.ShouldBe(new TopicPartition("some-topic", 42));
    }

    [Fact]
    public void ProduceTo_ShouldSetEndpointFromEnvelopeBasedTopicNameFunction()
    {
        KafkaProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo((IOutboundEnvelope<TestEventOne> _) => "some-topic");

        KafkaProducerEndpointConfiguration configuration = builder.Build();
        configuration.EndpointResolver.ShouldBeOfType<KafkaDynamicProducerEndpointResolver<TestEventOne>>();
        configuration.RawName.ShouldStartWith("dynamic-");
        KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)configuration.EndpointResolver.GetEndpoint(_envelope);
        endpoint.TopicPartition.ShouldBe(new TopicPartition("some-topic", Partition.Any));
    }

    [Fact]
    public void ProduceTo_ShouldSetEndpointFromMessageBasedTopicNameFunction()
    {
        KafkaProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo((TestEventOne? _) => "some-topic");

        KafkaProducerEndpointConfiguration configuration = builder.Build();
        configuration.EndpointResolver.ShouldBeOfType<KafkaDynamicProducerEndpointResolver<TestEventOne>>();
        configuration.RawName.ShouldStartWith("dynamic-");
        KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)configuration.EndpointResolver.GetEndpoint(_envelope);
        endpoint.TopicPartition.ShouldBe(new TopicPartition("some-topic", Partition.Any));
    }

    [Fact]
    public void ProduceTo_ShouldSetEndpointFromEnvelopeBasedTopicNameFunctionAndPartitionFunction()
    {
        KafkaProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo((IOutboundEnvelope<TestEventOne> _) => "some-topic", _ => 42);

        KafkaProducerEndpointConfiguration configuration = builder.Build();
        configuration.EndpointResolver.ShouldBeOfType<KafkaDynamicProducerEndpointResolver<TestEventOne>>();
        configuration.RawName.ShouldStartWith("dynamic-");
        KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)configuration.EndpointResolver.GetEndpoint(_envelope);
        endpoint.TopicPartition.ShouldBe(new TopicPartition("some-topic", new Partition(42)));
    }

    [Fact]
    public void ProduceTo_ShouldSetEndpointFromMessageBasedTopicNameFunctionAndPartitionFunction()
    {
        KafkaProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo((TestEventOne? _) => "some-topic", _ => 42);

        KafkaProducerEndpointConfiguration configuration = builder.Build();
        configuration.EndpointResolver.ShouldBeOfType<KafkaDynamicProducerEndpointResolver<TestEventOne>>();
        configuration.RawName.ShouldStartWith("dynamic-");
        KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)configuration.EndpointResolver.GetEndpoint(_envelope);
        endpoint.TopicPartition.ShouldBe(new TopicPartition("some-topic", new Partition(42)));
    }

    [Fact]
    public void ProduceTo_ShouldSetEndpointFromEnvelopeBasedTopicPartitionFunction()
    {
        KafkaProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo((IOutboundEnvelope<TestEventOne> _) => new TopicPartition("some-topic", 42));

        KafkaProducerEndpointConfiguration configuration = builder.Build();
        configuration.EndpointResolver.ShouldBeOfType<KafkaDynamicProducerEndpointResolver<TestEventOne>>();
        configuration.RawName.ShouldStartWith("dynamic-");
        KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)configuration.EndpointResolver.GetEndpoint(_envelope);
        endpoint.TopicPartition.ShouldBe(new TopicPartition("some-topic", new Partition(42)));
    }

    [Fact]
    public void ProduceTo_ShouldSetEndpointFromMessageBasedTopicPartitionFunction()
    {
        KafkaProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo((TestEventOne? _) => new TopicPartition("some-topic", 42));

        KafkaProducerEndpointConfiguration configuration = builder.Build();
        configuration.EndpointResolver.ShouldBeOfType<KafkaDynamicProducerEndpointResolver<TestEventOne>>();
        configuration.RawName.ShouldStartWith("dynamic-");
        KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)configuration.EndpointResolver.GetEndpoint(_envelope);
        endpoint.TopicPartition.ShouldBe(new TopicPartition("some-topic", new Partition(42)));
    }

    [Fact]
    public void ProduceTo_ShouldSetEndpointFromEnvelopeBasedTopicNameFormat()
    {
        KafkaProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo("some-topic-{0}", (IOutboundEnvelope<TestEventOne> _) => ["123"], _ => 42);

        KafkaProducerEndpointConfiguration configuration = builder.Build();
        configuration.EndpointResolver.ShouldBeOfType<KafkaDynamicProducerEndpointResolver<TestEventOne>>();
        configuration.RawName.ShouldBe("some-topic-{0}");
        KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)configuration.EndpointResolver.GetEndpoint(_envelope);
        endpoint.TopicPartition.ShouldBe(new TopicPartition("some-topic-123", new Partition(42)));
    }

    [Fact]
    public void ProduceTo_ShouldSetEndpointFromMessageBasedTopicNameFormat()
    {
        KafkaProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.ProduceTo("some-topic-{0}", (TestEventOne? _) => ["123"], _ => 42);

        KafkaProducerEndpointConfiguration configuration = builder.Build();
        configuration.EndpointResolver.ShouldBeOfType<KafkaDynamicProducerEndpointResolver<TestEventOne>>();
        configuration.RawName.ShouldBe("some-topic-{0}");
        KafkaProducerEndpoint endpoint = (KafkaProducerEndpoint)configuration.EndpointResolver.GetEndpoint(_envelope);
        endpoint.TopicPartition.ShouldBe(new TopicPartition("some-topic-123", new Partition(42)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ProduceTo_ShouldThrow_WhenTopicNameIsNotValid(string? topicName)
    {
        KafkaProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        Action act = () => builder.ProduceTo(topicName!);

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void ProduceTo_ShouldThrow_WhenPartitionIndexIsNotValid()
    {
        KafkaProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        Action act = () => builder.ProduceTo("test", -42);

        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void UseEndpointResolver_ShouldSetEndpoint()
    {
        KafkaProducerEndpointConfigurationBuilder<TestEventOne> builder = new(Substitute.For<IServiceProvider>());

        builder.UseEndpointResolver<TestTypedEndpointResolver>();

        KafkaProducerEndpointConfiguration configuration = builder.Build();
        configuration.EndpointResolver.ShouldBeOfType<KafkaDynamicProducerEndpointResolver<TestEventOne>>();
        configuration.RawName.ShouldStartWith("dynamic-TestTypedEndpointResolver-");
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Class used via DI")]
    private sealed class TestTypedEndpointResolver : IKafkaProducerEndpointResolver<TestEventOne>
    {
        public TopicPartition GetTopicPartition(IOutboundEnvelope<TestEventOne> envelope) => new("some-topic", 42);
    }
}
