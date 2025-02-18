// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class TestingHelperFixture : KafkaFixture
{
    public TestingHelperFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task WaitUntilAllMessagesAreConsumedAsync_ShouldWaitAllTopicsAndPartitions()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka(kafka => kafka.WithDefaultPartitionsCount(2)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1"))
                                .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo("topic2"))
                                .Produce<TestEventThree>(endpoint => endpoint.ProduceTo("topic3")))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom("topic1", "topic2", "topic3"))))
                .AddDelegateSubscriber<IIntegrationEvent>(_ => Task.Delay(Random.Shared.Next(5, 50)))
                .AddIntegrationSpy());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishAsync(new TestEventOne { ContentEventOne = $"{i}" });
            await publisher.PublishAsync(new TestEventTwo { ContentEventTwo = $"{i}" });
            await publisher.PublishAsync(new TestEventThree { ContentEventThree = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.GetConsumerGroup(DefaultGroupId).CommittedOffsets.ShouldBe(
            [
                new TopicPartitionOffset("topic1", 0, 3),
                new TopicPartitionOffset("topic1", 1, 2),
                new TopicPartitionOffset("topic2", 0, 3),
                new TopicPartitionOffset("topic2", 1, 2),
                new TopicPartitionOffset("topic3", 0, 3),
                new TopicPartitionOffset("topic3", 1, 2)
            ],
            ignoreOrder: true);
    }

    [Fact]
    public async Task WaitUntilAllMessagesAreConsumedAsync_ShouldWaitSpecifiedTopicsOnly()
    {
        TaskCompletionSource taskCompletionSource = new();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka(kafka => kafka.WithDefaultPartitionsCount(2)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1"))
                                .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo("topic2"))
                                .Produce<TestEventThree>(endpoint => endpoint.ProduceTo("topic3")))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom("topic1", "topic2", "topic3"))))
                .AddDelegateSubscriber<TestEventOne>(_ => Task.Delay(Random.Shared.Next(5, 50)))
                .AddDelegateSubscriber<TestEventTwo>(_ => Task.Delay(Random.Shared.Next(5, 50)))
                .AddDelegateSubscriber<TestEventThree>(_ => taskCompletionSource.Task)
                .AddIntegrationSpy());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 10; i++)
        {
            await publisher.PublishAsync(new TestEventOne { ContentEventOne = $"{i}" });
            await publisher.PublishAsync(new TestEventTwo { ContentEventTwo = $"{i}" });
            await publisher.PublishAsync(new TestEventThree { ContentEventThree = $"{i}" });
        }

        try
        {
            await Helper.WaitUntilAllMessagesAreConsumedAsync("topic1", "topic2");

            Helper.GetConsumerGroup(DefaultGroupId).CommittedOffsets.ShouldBe(
                [
                    new TopicPartitionOffset("topic1", 0, 5),
                    new TopicPartitionOffset("topic1", 1, 5),
                    new TopicPartitionOffset("topic2", 0, 5),
                    new TopicPartitionOffset("topic2", 1, 5)
                ],
                ignoreOrder: true);
        }
        finally
        {
            taskCompletionSource.SetResult();
        }
    }

    [Fact]
    public async Task WaitUntilAllMessagesAreConsumedAsync_ShouldWaitSpecifiedFriendlyEndpointNamesOnly()
    {
        TaskCompletionSource taskCompletionSource = new();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka(kafka => kafka.WithDefaultPartitionsCount(2)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<TestEventOne>("one", endpoint => endpoint.ProduceTo("topic1"))
                                .Produce<TestEventTwo>("two", endpoint => endpoint.ProduceTo("topic2"))
                                .Produce<TestEventThree>("three", endpoint => endpoint.ProduceTo("topic3")))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom("topic1", "topic2", "topic3"))))
                .AddDelegateSubscriber<TestEventOne>(_ => Task.Delay(Random.Shared.Next(5, 50)))
                .AddDelegateSubscriber<TestEventTwo>(_ => Task.Delay(Random.Shared.Next(5, 50)))
                .AddDelegateSubscriber<TestEventThree>(_ => taskCompletionSource.Task)
                .AddIntegrationSpy());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 10; i++)
        {
            await publisher.PublishAsync(new TestEventOne { ContentEventOne = $"{i}" });
            await publisher.PublishAsync(new TestEventTwo { ContentEventTwo = $"{i}" });
            await publisher.PublishAsync(new TestEventThree { ContentEventThree = $"{i}" });
        }

        try
        {
            await Helper.WaitUntilAllMessagesAreConsumedAsync("one", "two");

            Helper.GetConsumerGroup(DefaultGroupId).CommittedOffsets.ShouldBe(
                [
                    new TopicPartitionOffset("topic1", 0, 5),
                    new TopicPartitionOffset("topic1", 1, 5),
                    new TopicPartitionOffset("topic2", 0, 5),
                    new TopicPartitionOffset("topic2", 1, 5)
                ],
                ignoreOrder: true);
        }
        finally
        {
            taskCompletionSource.SetResult();
        }
    }
}
