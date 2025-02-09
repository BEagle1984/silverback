// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class BatchProcessingFixture
{
    [Fact]
    public async Task Batch_ShouldCreateConcurrentBatchPerPartition()
    {
        TestingCollection<List<TestEventOne>> receivedBatches = [];

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .CommitOffsetEach(1)
                                .Consume<TestEventOne>(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnableBatchProcessing(10))))
                .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch));

        async Task HandleBatch(IAsyncEnumerable<TestEventOne> batch)
        {
            List<TestEventOne> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message);
            }
        }

        IProducer producer = Helper.GetProducer(
            producer => producer
                .WithBootstrapServers("PLAINTEXT://e2e")
                .Produce<TestEventOne>(
                    endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .SetKafkaKey(envelope => envelope.Message?.ContentEventOne)));

        for (int i = 1; i <= 10; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 10);

        receivedBatches.Sum(messages => messages.Count).ShouldBe(10);
        receivedBatches.Count.ShouldBe(3);
    }

    [Fact]
    public async Task Batch_ShouldCreateConcurrentBatchPerPartition_WhenSubscribingMultipleTopicsFromSameConsumer()
    {
        TestingCollection<List<TestEventOne>> receivedBatches = [];

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .CommitOffsetEach(1)
                                .Consume<TestEventOne>(
                                    "consumer1",
                                    endpoint => endpoint
                                        .ConsumeFrom("topic1", "topic2")
                                        .EnableBatchProcessing(10))))
                .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch));

        async Task HandleBatch(IAsyncEnumerable<TestEventOne> batch)
        {
            List<TestEventOne> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message);
            }
        }

        IProducer producer1 = Helper.GetProducer(
            producer => producer
                .WithBootstrapServers("PLAINTEXT://e2e")
                .Produce<TestEventOne>(
                    endpoint => endpoint
                        .ProduceTo("topic1")
                        .SetKafkaKey(envelope => envelope.Message?.ContentEventOne)));
        IProducer producer2 = Helper.GetProducer(
            producer => producer
                .WithBootstrapServers("PLAINTEXT://e2e")
                .Produce<TestEventOne>(
                    endpoint => endpoint
                        .ProduceTo("topic2")
                        .SetKafkaKey(envelope => envelope.Message?.ContentEventOne)));

        for (int i = 1; i <= 10; i++)
        {
            await producer1.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
            await producer2.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 20);

        receivedBatches.Sum(messages => messages.Count).ShouldBe(20);
        receivedBatches.Count.ShouldBe(6);
    }

    [Fact]
    public async Task Batch_ShouldCreateSingleBatch_WhenPartitionsProcessedTogether()
    {
        TestingCollection<List<TestEventOne>> receivedBatches = [];

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .CommitOffsetEach(1)
                                .ProcessAllPartitionsTogether()
                                .Consume<TestEventOne>(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnableBatchProcessing(10))))
                .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch));

        async Task HandleBatch(IAsyncEnumerable<TestEventOne> batch)
        {
            List<TestEventOne> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 10; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 10);

        receivedBatches.Count.ShouldBe(1);
        receivedBatches.Sum(batch => batch.Count).ShouldBe(10);
    }

    [Fact]
    public async Task Batch_ShouldLimitParallelism_WhenConsumingFromMultiplePartitions()
    {
        TestingCollection<TestEventWithKafkaKey> receivedMessages = [];
        TaskCompletionSource<bool> taskCompletionSource = new();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .CommitOffsetEach(1)
                                .LimitParallelism(2)
                                .Consume<TestEventOne>(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnableBatchProcessing(2))))
                .AddDelegateSubscriber<IAsyncEnumerable<TestEventWithKafkaKey>>(HandleBatch));

        async ValueTask HandleBatch(IAsyncEnumerable<TestEventWithKafkaKey> batch)
        {
            await foreach (TestEventWithKafkaKey message in batch)
            {
                receivedMessages.Add(message);
                await taskCompletionSource.Task;
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 4; i++)
        {
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 1, Content = $"{i}" });
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 2, Content = $"{i}" });
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 3, Content = $"{i}" });
            await producer.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 4, Content = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedMessages.Count >= 2);
        await Task.Delay(100);

        try
        {
            receivedMessages.Count.ShouldBe(2);
        }
        finally
        {
            taskCompletionSource.SetResult(true);
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.Count.ShouldBe(16);
    }
}
