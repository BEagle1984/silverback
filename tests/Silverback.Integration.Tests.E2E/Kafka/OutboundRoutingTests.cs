// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.EndpointResolvers;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class OutboundRoutingTests : KafkaTestFixture
{
    public OutboundRoutingTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task OutboundRouting_AnyPartition_MessagesRoutedToRandomPartition()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        await publisher.PublishAsync(new TestEventOne { Content = "1" });
        await publisher.PublishAsync(new TestEventOne { Content = "2" });
        await publisher.PublishAsync(new TestEventOne { Content = "3" });
        await publisher.PublishAsync(new TestEventOne { Content = "4" });
        await publisher.PublishAsync(new TestEventOne { Content = "5" });

        DefaultTopic.Partitions
            .Where(partition => partition.Messages.Count > 0)
            .Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public async Task OutboundRouting_SpecificPartition_MessagesRoutedToSpecifiedPartition()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName, 3))))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        await publisher.PublishAsync(new TestEventOne { Content = "1" });
        await publisher.PublishAsync(new TestEventOne { Content = "2" });
        await publisher.PublishAsync(new TestEventOne { Content = "3" });
        await publisher.PublishAsync(new TestEventOne { Content = "4" });
        await publisher.PublishAsync(new TestEventOne { Content = "5" });

        DefaultTopic.Partitions[0].Messages.Count.Should().Be(0);
        DefaultTopic.Partitions[1].Messages.Count.Should().Be(0);
        DefaultTopic.Partitions[2].Messages.Count.Should().Be(0);
        DefaultTopic.Partitions[3].Messages.Count.Should().Be(5);
        DefaultTopic.Partitions[4].Messages.Count.Should().Be(0);
    }

    [Fact]
    public async Task OutboundRouting_AnyPartitionWithPartitionKey_MessagesRoutedToPredictablePartition()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = 1, Content = "1" });
        await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = 2, Content = "2" });
        await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = 1, Content = "3" });
        await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = 2, Content = "4" });
        await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = 3, Content = "5" });

        DefaultTopic.Partitions[0].Messages.Count.Should().Be(2);
        DefaultTopic.Partitions[1].Messages.Count.Should().Be(1);
        DefaultTopic.Partitions[2].Messages.Count.Should().Be(0);
        DefaultTopic.Partitions[3].Messages.Count.Should().Be(0);
        DefaultTopic.Partitions[4].Messages.Count.Should().Be(2);
    }

    [Fact]
    public async Task OutboundRouting_AnyPartitionWithNullPartitionKey_MessagesRoutedToRandomPartition()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = null, Content = "1" });
        await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = null, Content = "2" });
        await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = null, Content = "3" });
        await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = null, Content = "4" });
        await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = null, Content = "5" });

        DefaultTopic.Partitions
            .Where(partition => partition.Messages.Count > 0)
            .Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public async Task OutboundRouting_AnyPartitionWithNullOrEmptyPartitionKey_MessagesRoutedToRandomPartition()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        await publisher.PublishAsync(new TestEventWithStringKafkaKey { KafkaKey = null, Content = "1" });
        await publisher.PublishAsync(new TestEventWithStringKafkaKey { KafkaKey = string.Empty, Content = "2" });
        await publisher.PublishAsync(new TestEventWithStringKafkaKey { KafkaKey = string.Empty, Content = "3" });
        await publisher.PublishAsync(new TestEventWithStringKafkaKey { KafkaKey = string.Empty, Content = "4" });
        await publisher.PublishAsync(new TestEventWithStringKafkaKey { KafkaKey = null, Content = "5" });

        DefaultTopic.Partitions
            .Where(partition => partition.Messages.Count > 0)
            .Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public async Task OutboundRouting_SpecificPartitionWithPartitionKey_MessagesRoutedToSpecifiedPartition()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName, 3))))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = 1, Content = "1" });
        await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = 2, Content = "2" });
        await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = 1, Content = "3" });
        await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = 2, Content = "4" });
        await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = 3, Content = "5" });

        DefaultTopic.Partitions[0].Messages.Count.Should().Be(0);
        DefaultTopic.Partitions[1].Messages.Count.Should().Be(0);
        DefaultTopic.Partitions[2].Messages.Count.Should().Be(0);
        DefaultTopic.Partitions[3].Messages.Count.Should().Be(5);
        DefaultTopic.Partitions[4].Messages.Count.Should().Be(0);
    }

    [Fact]
    public async Task DynamicRouting_TopicFunction_MessagesRouted()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<TestEventOne>(
                                producer => producer
                                    .ProduceTo(
                                        testEventOne =>
                                        {
                                            switch (testEventOne?.Content)
                                            {
                                                case "1":
                                                    return "topic1";
                                                case "2":
                                                    return "topic2";
                                                case "3":
                                                    return "topic3";
                                                default:
                                                    throw new InvalidOperationException();
                                            }
                                        }))))
            .Run();

        IInMemoryTopic topic1 = Helper.GetTopic("topic1");
        IInMemoryTopic topic2 = Helper.GetTopic("topic2");
        IInMemoryTopic topic3 = Helper.GetTopic("topic3");
        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        await publisher.PublishAsync(new TestEventOne { Content = "1" });
        await publisher.PublishAsync(new TestEventOne { Content = "2" });
        await publisher.PublishAsync(new TestEventOne { Content = "3" });

        topic1.MessagesCount.Should().Be(1);
        topic2.MessagesCount.Should().Be(1);
        topic3.MessagesCount.Should().Be(1);

        await publisher.PublishAsync(new TestEventOne { Content = "1" });
        await publisher.PublishAsync(new TestEventOne { Content = "3" });

        topic1.MessagesCount.Should().Be(2);
        topic2.MessagesCount.Should().Be(1);
        topic3.MessagesCount.Should().Be(2);
    }

    [Fact]
    public async Task DynamicRouting_TopicAndPartitionFunctions_MessagesRouted()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<TestEventOne>(
                                producer => producer
                                    .ProduceTo(
                                        testEventOne =>
                                        {
                                            switch (testEventOne?.Content)
                                            {
                                                case "1":
                                                    return "topic1";
                                                case "2":
                                                    return "topic2";
                                                case "3":
                                                    return "topic3";
                                                default:
                                                    throw new InvalidOperationException();
                                            }
                                        },
                                        testEventOne =>
                                        {
                                            switch (testEventOne?.Content)
                                            {
                                                case "1":
                                                    return 2;
                                                case "2":
                                                    return 3;
                                                default:
                                                    return Partition.Any;
                                            }
                                        }))))
            .Run();

        IInMemoryTopic topic1 = Helper.GetTopic("topic1");
        IInMemoryTopic topic2 = Helper.GetTopic("topic2");
        IInMemoryTopic topic3 = Helper.GetTopic("topic3");
        IInMemoryPartition partition1 = topic1.Partitions[2];
        IInMemoryPartition partition2 = topic2.Partitions[3];

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        await publisher.PublishAsync(new TestEventOne { Content = "1" });
        await publisher.PublishAsync(new TestEventOne { Content = "2" });
        await publisher.PublishAsync(new TestEventOne { Content = "3" });

        topic1.MessagesCount.Should().Be(1);
        topic2.MessagesCount.Should().Be(1);
        topic3.MessagesCount.Should().Be(1);
        partition1.Messages.Count.Should().Be(1);
        partition2.Messages.Count.Should().Be(1);

        await publisher.PublishAsync(new TestEventOne { Content = "1" });
        await publisher.PublishAsync(new TestEventOne { Content = "3" });

        topic1.MessagesCount.Should().Be(2);
        topic2.MessagesCount.Should().Be(1);
        topic3.MessagesCount.Should().Be(2);
        partition1.Messages.Count.Should().Be(2);
        partition2.Messages.Count.Should().Be(1);
    }

    [Fact]
    public async Task DynamicRouting_TopicFormat_MessagesRouted()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<TestEventOne>(
                                producer => producer
                                    .ProduceTo(
                                        "topic{0}",
                                        testEventOne =>
                                        {
                                            switch (testEventOne?.Content)
                                            {
                                                case "1":
                                                    return new[] { "1" };
                                                case "2":
                                                    return new[] { "2" };
                                                case "3":
                                                    return new[] { "3" };
                                                default:
                                                    throw new InvalidOperationException();
                                            }
                                        }))))
            .Run();

        IInMemoryTopic topic1 = Helper.GetTopic("topic1");
        IInMemoryTopic topic2 = Helper.GetTopic("topic2");
        IInMemoryTopic topic3 = Helper.GetTopic("topic3");
        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        await publisher.PublishAsync(new TestEventOne { Content = "1" });
        await publisher.PublishAsync(new TestEventOne { Content = "2" });
        await publisher.PublishAsync(new TestEventOne { Content = "3" });

        topic1.MessagesCount.Should().Be(1);
        topic2.MessagesCount.Should().Be(1);
        topic3.MessagesCount.Should().Be(1);

        await publisher.PublishAsync(new TestEventOne { Content = "1" });
        await publisher.PublishAsync(new TestEventOne { Content = "3" });

        topic1.MessagesCount.Should().Be(2);
        topic2.MessagesCount.Should().Be(1);
        topic3.MessagesCount.Should().Be(2);
    }

    [Fact]
    public async Task DynamicRouting_CustomEndpointResolver_MessagesRouted()
    {
        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSingleton<TestEndpointResolver>()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<TestEventOne>(
                                endpoint => endpoint
                                    .UseEndpointResolver<TestEndpointResolver>())))
            .Run();

        IInMemoryTopic topic1 = Helper.GetTopic("topic1");
        IInMemoryTopic topic2 = Helper.GetTopic("topic2");
        IInMemoryTopic topic3 = Helper.GetTopic("topic3");
        IInMemoryPartition partition1 = topic1.Partitions[2];
        IInMemoryPartition partition2 = topic2.Partitions[3];

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        await publisher.PublishAsync(new TestEventOne { Content = "1" });
        await publisher.PublishAsync(new TestEventOne { Content = "2" });
        await publisher.PublishAsync(new TestEventOne { Content = "3" });

        topic1.MessagesCount.Should().Be(1);
        topic2.MessagesCount.Should().Be(1);
        topic3.MessagesCount.Should().Be(1);
        partition1.Messages.Count.Should().Be(1);
        partition2.Messages.Count.Should().Be(1);

        await publisher.PublishAsync(new TestEventOne { Content = "1" });
        await publisher.PublishAsync(new TestEventOne { Content = "3" });

        topic1.MessagesCount.Should().Be(2);
        topic2.MessagesCount.Should().Be(1);
        topic3.MessagesCount.Should().Be(2);
        partition1.Messages.Count.Should().Be(2);
        partition2.Messages.Count.Should().Be(1);
    }

    [SuppressMessage("", "CA1812", Justification = "Class used via DI")]
    private sealed class TestEndpointResolver : IKafkaProducerEndpointResolver<TestEventOne>
    {
        public TopicPartition GetTopicPartition(TestEventOne? message) => new(GetTopic(message), GetPartition(message));

        private static string GetTopic(TestEventOne? testEventOne) =>
            testEventOne?.Content switch
            {
                "1" => "topic1",
                "2" => "topic2",
                "3" => "topic3",
                _ => throw new InvalidOperationException()
            };

        private static Partition GetPartition(TestEventOne? testEventOne) =>
            testEventOne?.Content switch
            {
                "1" => 2,
                "2" => 3,
                _ => Partition.Any
            };
    }
}
