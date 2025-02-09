// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
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
                                            message => message?.ContentEventOne switch
                                            {
                                                "1" => "topic1",
                                                "2" => "topic2",
                                                "3" => "topic3",
                                                _ => throw new InvalidOperationException()
                                            })))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "2" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" });

        IInMemoryTopic topic1 = Helper.GetTopic("topic1");
        IInMemoryTopic topic2 = Helper.GetTopic("topic2");
        IInMemoryTopic topic3 = Helper.GetTopic("topic3");

        topic1.MessagesCount.ShouldBe(1);
        topic2.MessagesCount.ShouldBe(1);
        topic3.MessagesCount.ShouldBe(1);

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" });

        topic1.MessagesCount.ShouldBe(2);
        topic2.MessagesCount.ShouldBe(1);
        topic3.MessagesCount.ShouldBe(2);
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldProduce_WhenTopicAndPartitionFunctionsAreSet()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
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
                                            message => message?.ContentEventOne switch
                                            {
                                                "1" => "topic1",
                                                "2" => "topic2",
                                                "3" => "topic3",
                                                _ => throw new InvalidOperationException(),
                                            },
                                            message => message?.ContentEventOne switch
                                            {
                                                "1" => 2,
                                                "2" => 3,
                                                _ => Partition.Any
                                            })))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "2" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" });

        IInMemoryTopic topic1 = Helper.GetTopic("topic1");
        IInMemoryTopic topic2 = Helper.GetTopic("topic2");
        IInMemoryTopic topic3 = Helper.GetTopic("topic3");
        IInMemoryPartition partition1 = topic1.Partitions[2];
        IInMemoryPartition partition2 = topic2.Partitions[3];

        topic1.MessagesCount.ShouldBe(1);
        topic2.MessagesCount.ShouldBe(1);
        topic3.MessagesCount.ShouldBe(1);
        partition1.TotalMessagesCount.ShouldBe(1);
        partition2.TotalMessagesCount.ShouldBe(1);

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" });

        topic1.MessagesCount.ShouldBe(2);
        topic2.MessagesCount.ShouldBe(1);
        topic3.MessagesCount.ShouldBe(2);
        partition1.TotalMessagesCount.ShouldBe(2);
        partition2.TotalMessagesCount.ShouldBe(1);
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldProduce_WhenTopicFormatIsSet()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
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
                                            message => message?.ContentEventOne switch
                                            {
                                                "1" => ["1"],
                                                "2" => ["2"],
                                                "3" => ["3"],
                                                _ => throw new InvalidOperationException(),
                                            })))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "2" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" });

        IInMemoryTopic topic1 = Helper.GetTopic("topic1");
        IInMemoryTopic topic2 = Helper.GetTopic("topic2");
        IInMemoryTopic topic3 = Helper.GetTopic("topic3");

        topic1.MessagesCount.ShouldBe(1);
        topic2.MessagesCount.ShouldBe(1);
        topic3.MessagesCount.ShouldBe(1);

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" });

        topic1.MessagesCount.ShouldBe(2);
        topic2.MessagesCount.ShouldBe(1);
        topic3.MessagesCount.ShouldBe(2);
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldProduce_WhenMessageBasedEndpointResolverIsSet()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSingleton<MessageBasedEndpointResolver>()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<TestEventOne>(endpoint => endpoint.UseEndpointResolver<MessageBasedEndpointResolver>()))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "2" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" });

        IInMemoryTopic topic1 = Helper.GetTopic("topic1");
        IInMemoryTopic topic2 = Helper.GetTopic("topic2");
        IInMemoryTopic topic3 = Helper.GetTopic("topic3");
        IInMemoryPartition partition1 = topic1.Partitions[2];
        IInMemoryPartition partition2 = topic2.Partitions[3];

        topic1.MessagesCount.ShouldBe(1);
        topic2.MessagesCount.ShouldBe(1);
        topic3.MessagesCount.ShouldBe(1);
        partition1.TotalMessagesCount.ShouldBe(1);
        partition2.TotalMessagesCount.ShouldBe(1);

        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" });

        topic1.MessagesCount.ShouldBe(2);
        topic2.MessagesCount.ShouldBe(1);
        topic3.MessagesCount.ShouldBe(2);
        partition1.TotalMessagesCount.ShouldBe(2);
        partition2.TotalMessagesCount.ShouldBe(1);
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldProduce_WhenHeadersBasedEndpointResolverIsSet()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSingleton<HeaderBasedEndpointResolver>()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(5)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<TestEventOne>(endpoint => endpoint.UseEndpointResolver<HeaderBasedEndpointResolver>()))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.WrapAndPublishAsync(
            new TestEventOne(),
            envelope => envelope
                .AddHeader("x-where-to-go-topic", "topic1")
                .AddHeader("x-where-to-go-partition", 2));
        await publisher.WrapAndPublishAsync(
            new TestEventOne(),
            envelope => envelope
                .AddHeader("x-where-to-go-topic", "topic2")
                .AddHeader("x-where-to-go-partition", 3));

        IInMemoryPartition partition1 = Helper.GetTopic("topic1").Partitions[2];
        IInMemoryPartition partition2 = Helper.GetTopic("topic2").Partitions[3];

        partition1.TotalMessagesCount.ShouldBe(1);
        partition2.TotalMessagesCount.ShouldBe(1);

        await publisher.WrapAndPublishBatchAsync(
            [
                new TestEventOne(),
                new TestEventOne()
            ],
            envelope => envelope
                .AddHeader("x-where-to-go-topic", "topic1")
                .AddHeader("x-where-to-go-partition", 2));

        partition1.TotalMessagesCount.ShouldBe(3);
        partition2.TotalMessagesCount.ShouldBe(1);
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldProduceToDynamicEndpointSetViaEnvelopeExtensions()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer.Produce<IIntegrationEvent>(
                                endpoint => endpoint.ProduceToDynamicTopic()))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.WrapAndPublishAsync(
            new TestEventOne(),
            envelope => envelope.SetKafkaDestinationTopic("topic1"));
        await publisher.WrapAndPublishBatchAsync(
            new IIntegrationEvent?[]
            {
                new TestEventOne(),
                null,
                new TestEventTwo(),
                new TestEventOne()
            },
            envelope => envelope.SetKafkaDestinationTopic(envelope.MessageType == typeof(TestEventOne) ? "topic1" : "topic2"));

        Helper.GetTopic("topic1").MessagesCount.ShouldBe(3);
        Helper.GetTopic("topic2").MessagesCount.ShouldBe(2);
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldProduceTombstoneToDynamicEndpoint()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer.Produce<IIntegrationEvent>(
                                endpoint => endpoint
                                    .ProduceTo((IIntegrationEvent? _) => DefaultTopicName)))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(new Tombstone<TestEventOne>("42"));

        DefaultTopic.MessagesCount.ShouldBe(1);
        DefaultTopic.GetAllMessages()[0].Value.ShouldBeNull();
        DefaultTopic.GetAllMessages()[0].Key.ShouldBe("42"u8.ToArray());
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldProduceCollectionToDynamicEndpoint()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer.Produce<IIntegrationEvent>(
                                endpoint => endpoint
                                    .ProduceTo(
                                        (IIntegrationEvent? message) =>
                                            message is TestEventOne ? "topic1" : "topic2")))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.WrapAndPublishBatchAsync(
            new IIntegrationEvent[]
            {
                new TestEventOne(),
                new TestEventTwo(),
                new TestEventOne()
            });

        Helper.GetTopic("topic1").MessagesCount.ShouldBe(2);
        Helper.GetTopic("topic2").MessagesCount.ShouldBe(1);
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldProduceCollectionWithTombstonesToDynamicEndpoint()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer.Produce<IIntegrationEvent>(
                                endpoint => endpoint
                                    .ProduceTo(
                                        (IIntegrationEvent? message) =>
                                            message == null ? "tombstones" : "events")))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.WrapAndPublishBatchAsync(
            new IIntegrationEvent?[]
            {
                new TestEventOne(),
                null,
                new TestEventTwo(),
                null,
                null
            });

        Helper.GetTopic("events").MessagesCount.ShouldBe(2);
        Helper.GetTopic("tombstones").MessagesCount.ShouldBe(3);
    }

    private sealed class MessageBasedEndpointResolver : IKafkaProducerEndpointResolver<TestEventOne>
    {
        public TopicPartition GetTopicPartition(IOutboundEnvelope<TestEventOne> envelope) => new(
            GetTopic(envelope.Message),
            GetPartition(envelope.Message));

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

    private sealed class HeaderBasedEndpointResolver : IKafkaProducerEndpointResolver<TestEventOne>
    {
        public TopicPartition GetTopicPartition(IOutboundEnvelope<TestEventOne> envelope) => new(
            envelope.Headers.GetValue("x-where-to-go-topic"),
            envelope.Headers.GetValueOrDefault<int>("x-where-to-go-partition"));
    }
}
