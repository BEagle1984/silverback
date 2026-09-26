// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class TestingHelperTests : KafkaTests
{
    public TestingHelperTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(10, false)]
    [InlineData(-1, false)]
    public async Task WaitUntilAllMessagesAreConsumedAsync_ShouldWaitForProcessing_WithoutWaitingForCommit(
        int commitOffsetEach,
        bool clientSideOffsetStore)
    {
        TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int processed = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(kafka => kafka.WithDefaultPartitionsCount(1).OverrideAutoCommitIntervalMs(60000))
                .AddInMemoryKafkaOffsetStore())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer =>
                {
                    consumer
                        .WithGroupId(DefaultGroupId)
                        .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName));

                    if (commitOffsetEach == 0)
                        consumer.DisableOffsetsCommit();
                    else if (commitOffsetEach > 0)
                        consumer.CommitOffsetEach(commitOffsetEach);

                    if (clientSideOffsetStore)
                        consumer.StoreOffsetsClientSide(store => store.UseMemory());
                }))
            .AddDelegateSubscriber<TestEventOne>(async Task (_) =>
            {
                entered.TrySetResult();
                await release.Task;
                Interlocked.Increment(ref processed);
            }));

        await Helper.GetProducerForEndpoint(DefaultTopicName).ProduceAsync(new TestEventOne());
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Task wait = Helper.WaitUntilAllMessagesAreConsumedAsync(TimeSpan.FromSeconds(5)).AsTask();

        try
        {
            await Should.ThrowAsync<TimeoutException>(() => wait.WaitAsync(TimeSpan.FromMilliseconds(100)));
            Volatile.Read(ref processed).ShouldBe(0);
        }
        finally
        {
            release.TrySetResult();
        }

        await wait;

        Volatile.Read(ref processed).ShouldBe(1);
        DefaultConsumerGroup.CommittedOffsets.ShouldBeEmpty();

        await Should.ThrowAsync<TimeoutException>(() =>
            Helper.WaitUntilAllMessagesAreCommittedAsync(TimeSpan.FromMilliseconds(100)).AsTask());

        DefaultConsumerGroup.CommittedOffsets.ShouldBeEmpty();
    }

    [Fact]
    public async Task WaitUntilAllMessagesAreCommittedAsync_ShouldWaitForBrokerCommit_AfterProcessingCompletes()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka(kafka => kafka.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(10)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

        await Helper.GetProducerForEndpoint(DefaultTopicName).ProduceAsync(new TestEventOne());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Task wait = Helper.WaitUntilAllMessagesAreCommittedAsync(TimeSpan.FromSeconds(5)).AsTask();

        await Should.ThrowAsync<TimeoutException>(() => wait.WaitAsync(TimeSpan.FromMilliseconds(100)));
        DefaultConsumerGroup.CommittedOffsets.ShouldBeEmpty();

        Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().Single().Client.Commit();
        await wait;

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(1);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WaitUntilAllMessagesAreCommittedAsync_ShouldRespectCancellation(bool throwTimeoutException)
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka()));

        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        if (throwTimeoutException)
        {
            await Should.ThrowAsync<TimeoutException>(() =>
                Helper.WaitUntilAllMessagesAreCommittedAsync(cancellation.Token).AsTask());
        }
        else
        {
            await Helper.WaitUntilAllMessagesAreCommittedAsync(false, cancellation.Token);
        }
    }

    [Fact]
    public async Task WaitUntilAllMessagesAreCommittedAsync_ShouldWaitAllTopicsAndPartitions()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka(kafka => kafka.WithDefaultPartitionsCount(2)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1"))
                    .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo("topic2"))
                    .Produce<TestEventThree>(endpoint => endpoint.ProduceTo("topic3")))
                .AddConsumer(consumer => consumer
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

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        Helper.GetConsumerGroup(DefaultGroupId).CommittedOffsets.ShouldBe(
            [
                new TopicPartitionOffset("topic1", 0, 3),
                new TopicPartitionOffset("topic1", 1, 2),
                new TopicPartitionOffset("topic2", 0, 3),
                new TopicPartitionOffset("topic2", 1, 2),
                new TopicPartitionOffset("topic3", 0, 3),
                new TopicPartitionOffset("topic3", 1, 2)
            ],
            true);
    }

    [Fact]
    public async Task WaitUntilAllMessagesAreCommittedAsync_ShouldWaitSpecifiedTopicsOnly()
    {
        TaskCompletionSource taskCompletionSource = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka(kafka => kafka.WithDefaultPartitionsCount(2)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1"))
                    .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo("topic2"))
                    .Produce<TestEventThree>(endpoint => endpoint.ProduceTo("topic3")))
                .AddConsumer(consumer => consumer
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
            await Helper.WaitUntilAllMessagesAreCommittedAsync("topic1", "topic2");

            Helper.GetConsumerGroup(DefaultGroupId).CommittedOffsets.ShouldBe(
                [
                    new TopicPartitionOffset("topic1", 0, 5),
                    new TopicPartitionOffset("topic1", 1, 5),
                    new TopicPartitionOffset("topic2", 0, 5),
                    new TopicPartitionOffset("topic2", 1, 5)
                ],
                true);
        }
        finally
        {
            taskCompletionSource.SetResult();
        }
    }

    [Fact]
    public async Task WaitUntilAllMessagesAreCommittedAsync_ShouldWaitSpecifiedFriendlyEndpointNamesOnly()
    {
        TaskCompletionSource taskCompletionSource = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka(kafka => kafka.WithDefaultPartitionsCount(2)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<TestEventOne>("one", endpoint => endpoint.ProduceTo("topic1"))
                    .Produce<TestEventTwo>("two", endpoint => endpoint.ProduceTo("topic2"))
                    .Produce<TestEventThree>("three", endpoint => endpoint.ProduceTo("topic3")))
                .AddConsumer(consumer => consumer
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
            await Helper.WaitUntilAllMessagesAreCommittedAsync("one", "two");

            Helper.GetConsumerGroup(DefaultGroupId).CommittedOffsets.ShouldBe(
                [
                    new TopicPartitionOffset("topic1", 0, 5),
                    new TopicPartitionOffset("topic1", 1, 5),
                    new TopicPartitionOffset("topic2", 0, 5),
                    new TopicPartitionOffset("topic2", 1, 5)
                ],
                true);
        }
        finally
        {
            taskCompletionSource.SetResult();
        }
    }
}
