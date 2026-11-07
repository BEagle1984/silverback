// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;
using NSubstitute;
using Shouldly;
using Silverback.Collections;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Producing.EndpointResolvers;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Testing.Messaging.Broker.Kafka;

public class MockedConfluentAdminClientFactoryTests
{
    private const string BootstrapServers = "PLAINTEXT://test:9092";

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1);

    private readonly InMemoryTopicCollection _topics = new(new MockedKafkaOptions());

    private readonly IProducerCollection _producers = Substitute.For<IProducerCollection>();

    private readonly IConsumerCollection _consumers = Substitute.For<IConsumerCollection>();

    private readonly MockedConfluentAdminClientFactory _factory;

    public MockedConfluentAdminClientFactoryTests()
    {
        _factory = new MockedConfluentAdminClientFactory(_topics, _producers, _consumers);
    }

    [Fact]
    public void GetClient_ShouldReturnClient()
    {
        using IAdminClient client = _factory.GetClient(new AdminClientConfig { BootstrapServers = BootstrapServers });

        client.ShouldNotBeNull();
    }

    [Fact]
    [SuppressMessage("ReSharper", "NotDisposedResource", Justification = "The enumerator is consumed by the factory")]
    public void GetClientForProducerEndpoint_ShouldReturnClientUsingProducerConfiguration()
    {
        _topics.TryCreate("topic", BootstrapServers, 3);
        KafkaTransactionalProducer producer = new(
            "producer1",
            new KafkaProducerConfiguration
            {
                BootstrapServers = BootstrapServers,
                Endpoints = new ValueReadOnlyCollection<KafkaProducerEndpointConfiguration>(
                [
                    new KafkaProducerEndpointConfiguration
                    {
                        EndpointResolver = new KafkaStaticProducerEndpointResolver("topic"),
                        FriendlyName = "endpoint"
                    }
                ])
            },
            Substitute.For<IKafkaTransactionalProducerCollection>());
        _producers.GetEnumerator().Returns(_ => new List<IProducer> { producer }.GetEnumerator());

        using IAdminClient client = _factory.GetClientForProducerEndpoint("endpoint");

        client.GetMetadata(Timeout).Topics.ShouldHaveSingleItem().Topic.ShouldBe("topic");
    }

    [Fact]
    [SuppressMessage("ReSharper", "NotDisposedResource", Justification = "The enumerator is consumed by the factory")]
    public void GetClientForProducer_ShouldReturnClientUsingProducerConfiguration()
    {
        _topics.TryCreate("topic", BootstrapServers, 3);
        KafkaTransactionalProducer producer = new(
            "producer1",
            new KafkaProducerConfiguration
            {
                BootstrapServers = BootstrapServers,
                Endpoints = new ValueReadOnlyCollection<KafkaProducerEndpointConfiguration>(
                [
                    new KafkaProducerEndpointConfiguration
                    {
                        EndpointResolver = new KafkaStaticProducerEndpointResolver("topic")
                    }
                ])
            },
            Substitute.For<IKafkaTransactionalProducerCollection>());
        _producers.GetEnumerator().Returns(_ => new List<IProducer> { producer }.GetEnumerator());

        using IAdminClient client = _factory.GetClientForProducer("producer1");

        client.GetMetadata(Timeout).Topics.ShouldHaveSingleItem().Topic.ShouldBe("topic");
    }

    [Fact]
    public void GetClientForConsumerEndpoint_ShouldReturnClientUsingConsumerConfiguration()
    {
        _topics.TryCreate("topic", BootstrapServers, 3);
        SetConsumer(new KafkaConsumerEndpointConfiguration { FriendlyName = "endpoint" });

        using IAdminClient client = _factory.GetClientForConsumerEndpoint("endpoint");

        client.GetMetadata(Timeout).Topics.ShouldHaveSingleItem().Topic.ShouldBe("topic");
    }

    [Fact]
    public void GetClientForConsumer_ShouldReturnClientUsingConsumerConfiguration()
    {
        _topics.TryCreate("topic", BootstrapServers, 3);
        SetConsumer(new KafkaConsumerEndpointConfiguration());

        using IAdminClient client = _factory.GetClientForConsumer("consumer1");

        client.GetMetadata(Timeout).Topics.ShouldHaveSingleItem().Topic.ShouldBe("topic");
    }

    [SuppressMessage("ReSharper", "NotDisposedResource", Justification = "The enumerator is consumed by the factory")]
    private void SetConsumer(KafkaConsumerEndpointConfiguration endpoint)
    {
        IKafkaConsumer consumer = Substitute.For<IKafkaConsumer>();
        consumer.Name.Returns("consumer1");
        consumer.Configuration.Returns(
            new KafkaConsumerConfiguration
            {
                BootstrapServers = BootstrapServers
            });
        consumer.EndpointsConfiguration.Returns([endpoint]);
        ((IEnumerable)_consumers).GetEnumerator().Returns(_ => new IConsumer[] { consumer }.GetEnumerator());
    }
}
