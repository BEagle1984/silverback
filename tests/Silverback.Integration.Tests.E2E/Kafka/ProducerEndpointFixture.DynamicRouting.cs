// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ProducerEndpointFixture
{
    [Fact]
    public async Task ProducerEndpoint_ShouldProduce_WhenTopicFunctionIsSet()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<TestEventOne>(
                                    endpoint => endpoint
                                        .ProduceTo(
                                            message =>
                                            {
                                                switch (message?.ContentEventOne)
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
                                            })))));

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "2" });
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "3" });

        IInMemoryTopic topic1 = Helper.GetTopic("topic1");
        IInMemoryTopic topic2 = Helper.GetTopic("topic2");
        IInMemoryTopic topic3 = Helper.GetTopic("topic3");

        topic1.MessagesCount.Should().Be(1);
        topic2.MessagesCount.Should().Be(1);
        topic3.MessagesCount.Should().Be(1);

        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "3" });

        topic1.MessagesCount.Should().Be(2);
        topic2.MessagesCount.Should().Be(1);
        topic3.MessagesCount.Should().Be(2);
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldProduce_WhenTopicAndPartitionFunctionsAreSet()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<TestEventOne>(
                                    endpoint => endpoint
                                        .ProduceTo(
                                            message =>
                                            {
                                                switch (message?.ContentEventOne)
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
                                            message =>
                                            {
                                                switch (message?.ContentEventOne)
                                                {
                                                    case "1":
                                                        return 2;
                                                    case "2":
                                                        return 3;
                                                    default:
                                                        return Partition.Any;
                                                }
                                            })))));

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "2" });
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "3" });

        IInMemoryTopic topic1 = Helper.GetTopic("topic1");
        IInMemoryTopic topic2 = Helper.GetTopic("topic2");
        IInMemoryTopic topic3 = Helper.GetTopic("topic3");
        IInMemoryPartition partition1 = topic1.Partitions[2];
        IInMemoryPartition partition2 = topic2.Partitions[3];

        topic1.MessagesCount.Should().Be(1);
        topic2.MessagesCount.Should().Be(1);
        topic3.MessagesCount.Should().Be(1);
        partition1.Messages.Count.Should().Be(1);
        partition2.Messages.Count.Should().Be(1);

        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "3" });

        topic1.MessagesCount.Should().Be(2);
        topic2.MessagesCount.Should().Be(1);
        topic3.MessagesCount.Should().Be(2);
        partition1.Messages.Count.Should().Be(2);
        partition2.Messages.Count.Should().Be(1);
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldProduce_WhenTopicFormatIsSet()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<TestEventOne>(
                                    endpoint => endpoint
                                        .ProduceTo(
                                            "topic{0}",
                                            message =>
                                            {
                                                switch (message?.ContentEventOne)
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
                                            })))));

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "2" });
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "3" });

        IInMemoryTopic topic1 = Helper.GetTopic("topic1");
        IInMemoryTopic topic2 = Helper.GetTopic("topic2");
        IInMemoryTopic topic3 = Helper.GetTopic("topic3");

        topic1.MessagesCount.Should().Be(1);
        topic2.MessagesCount.Should().Be(1);
        topic3.MessagesCount.Should().Be(1);

        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "3" });

        topic1.MessagesCount.Should().Be(2);
        topic2.MessagesCount.Should().Be(1);
        topic3.MessagesCount.Should().Be(2);
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldProduce_WhenCustomEndpointResolverIsSet()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSingleton<TestEndpointResolver>()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<TestEventOne>(endpoint => endpoint.UseEndpointResolver<TestEndpointResolver>()))));

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "2" });
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "3" });

        IInMemoryTopic topic1 = Helper.GetTopic("topic1");
        IInMemoryTopic topic2 = Helper.GetTopic("topic2");
        IInMemoryTopic topic3 = Helper.GetTopic("topic3");
        IInMemoryPartition partition1 = topic1.Partitions[2];
        IInMemoryPartition partition2 = topic2.Partitions[3];

        topic1.MessagesCount.Should().Be(1);
        topic2.MessagesCount.Should().Be(1);
        topic3.MessagesCount.Should().Be(1);
        partition1.Messages.Count.Should().Be(1);
        partition2.Messages.Count.Should().Be(1);

        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "3" });

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

        private static string GetTopic(TestEventOne? message) =>
            message?.ContentEventOne switch
            {
                "1" => "topic1",
                "2" => "topic2",
                "3" => "topic3",
                _ => throw new InvalidOperationException()
            };

        private static Partition GetPartition(TestEventOne? message) =>
            message?.ContentEventOne switch
            {
                "1" => 2,
                "2" => 3,
                _ => Partition.Any
            };
    }
}
