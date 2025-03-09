// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class BatchProcessingFixture : KafkaFixture
{
    public BatchProcessingFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Batch_ShouldConsumeAndCommitInBatch_WhenSubscribingToStream()
    {
        TestingCollection<List<TestEventOne>> receivedBatches = [];
        int completedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
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
                .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(HandleBatch));

        void HandleBatch(IMessageStreamEnumerable<TestEventOne> batch)
        {
            List<TestEventOne> list = [];
            receivedBatches.Add(list);

            foreach (TestEventOne message in batch)
            {
                list.Add(message);
            }

            Interlocked.Increment(ref completedBatches);
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 15; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15);

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].Count.ShouldBe(10);
        receivedBatches[1].Count.ShouldBe(5);
        completedBatches.ShouldBe(1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(10);

        for (int i = 16; i <= 20; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].Count.ShouldBe(10);
        receivedBatches[1].Count.ShouldBe(10);
        completedBatches.ShouldBe(2);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(20);
    }

    [Fact]
    public async Task Batch_ShouldConsumeAndCommitInBatch_WhenSubscribingToEnvelopesStream()
    {
        TestingCollection<List<TestEventOne>> receivedBatches = [];
        int completedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
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
                .AddDelegateSubscriber<IMessageStreamEnumerable<IInboundEnvelope<TestEventOne>>>(HandleBatch));

        void HandleBatch(IMessageStreamEnumerable<IInboundEnvelope<TestEventOne>> batch)
        {
            List<TestEventOne> list = [];
            receivedBatches.Add(list);

            foreach (IInboundEnvelope<TestEventOne> envelope in batch)
            {
                list.Add(envelope.Message!);
            }

            Interlocked.Increment(ref completedBatches);
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 15; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15);

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].Count.ShouldBe(10);
        receivedBatches[1].Count.ShouldBe(5);
        completedBatches.ShouldBe(1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(10);

        for (int i = 16; i <= 20; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].Count.ShouldBe(10);
        receivedBatches[1].Count.ShouldBe(10);
        completedBatches.ShouldBe(2);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(20);
    }

    [Fact]
    public async Task Batch_ShouldConsumeAndCommitInBatch_WhenSubscribingToStreamAndEnumeratingAsynchronously()
    {
        TestingCollection<List<TestEventOne>> receivedBatches = [];
        int completedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
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
                .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(HandleBatch));

        async Task HandleBatch(IMessageStreamEnumerable<TestEventOne> batch)
        {
            List<TestEventOne> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message);
            }

            Interlocked.Increment(ref completedBatches);
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 15; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15);

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].Count.ShouldBe(10);
        receivedBatches[1].Count.ShouldBe(5);
        completedBatches.ShouldBe(1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(10);

        for (int i = 16; i <= 20; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].Count.ShouldBe(10);
        receivedBatches[1].Count.ShouldBe(10);
        completedBatches.ShouldBe(2);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(20);
    }

    [Fact]
    public async Task Batch_ShouldConsumeAndCommitInBatch_WhenSubscribingToEnumerable()
    {
        TestingCollection<List<TestEventOne>> receivedBatches = [];
        int completedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
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
                .AddDelegateSubscriber<IEnumerable<TestEventOne>>(HandleBatch));

        void HandleBatch(IEnumerable<TestEventOne> batch)
        {
            List<TestEventOne> list = [];
            receivedBatches.Add(list);

            foreach (TestEventOne message in batch)
            {
                list.Add(message);
            }

            Interlocked.Increment(ref completedBatches);
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 15; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15);

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].Count.ShouldBe(10);
        receivedBatches[1].Count.ShouldBe(5);
        completedBatches.ShouldBe(1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(10);

        for (int i = 16; i <= 20; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].Count.ShouldBe(10);
        receivedBatches[1].Count.ShouldBe(10);
        completedBatches.ShouldBe(2);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(20);
    }

    [Fact]
    public async Task Batch_ShouldConsumeAndCommitInBatch_WhenSubscribingToAsyncEnumerable()
    {
        TestingCollection<List<TestEventOne>> receivedBatches = [];
        int completedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
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

        async ValueTask HandleBatch(IAsyncEnumerable<TestEventOne> batch)
        {
            List<TestEventOne> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message);
            }

            Interlocked.Increment(ref completedBatches);
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 15; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15);

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].Count.ShouldBe(10);
        receivedBatches[1].Count.ShouldBe(5);
        completedBatches.ShouldBe(1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(10);

        for (int i = 16; i <= 20; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].Count.ShouldBe(10);
        receivedBatches[1].Count.ShouldBe(10);
        completedBatches.ShouldBe(2);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(20);
    }

    [Fact]
    public async Task Batch_ShouldConsumeAndCommitInBatch_WhenSubscribingToObservable()
    {
        TestingCollection<List<TestEventOne>> receivedBatches = [];
        int completedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .AsObservable()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
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
                .AddDelegateSubscriber<IObservable<TestEventOne>>(HandleBatch));

        void HandleBatch(IObservable<TestEventOne> batch)
        {
            List<TestEventOne> list = [];
            receivedBatches.Add(list);

            batch.Subscribe(list.Add, () => Interlocked.Increment(ref completedBatches));
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 15; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15);

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].Count.ShouldBe(10);
        receivedBatches[1].Count.ShouldBe(5);
        completedBatches.ShouldBe(1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(10);

        for (int i = 16; i <= 20; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].Count.ShouldBe(10);
        receivedBatches[1].Count.ShouldBe(10);
        completedBatches.ShouldBe(2);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(20);
    }

    // This is to reproduce an issue that caused a deadlock, since the SubscribedMethodInvoker wasn't
    // properly forcing the asynchronous run of the async methods (with an additional Task.Run).
    [Fact]
    public async Task Batch_ShouldConsumeAndCommitInBatch_WhenAsyncSubscriberIsEnumeratingSynchronously()
    {
        TestingCollection<List<TestEventOne>> receivedBatches = [];
        int completedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
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
                .AddDelegateSubscriber<IEnumerable<TestEventOne>>(HandleBatch));

        Task HandleBatch(IEnumerable<TestEventOne> batch)
        {
            List<TestEventOne> list = [];
            receivedBatches.Add(list);

            foreach (TestEventOne message in batch)
            {
                list.Add(message);
            }

            Interlocked.Increment(ref completedBatches);

            // Return a Task, to trick the SubscribedMethodInvoker into thinking that this
            // is an asynchronous method, while still enumerating synchronously
            return Task.CompletedTask;
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 15; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15);

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].Count.ShouldBe(10);
        receivedBatches[1].Count.ShouldBe(5);
        completedBatches.ShouldBe(1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(10);

        for (int i = 16; i <= 20; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].Count.ShouldBe(10);
        receivedBatches[1].Count.ShouldBe(10);
        completedBatches.ShouldBe(2);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(20);
    }

    [Fact]
    public async Task Batch_ShouldRouteToMatchingSubscriberOnly_WhenSubscribingToStream()
    {
        int receivedBatches1 = 0;
        int receivedBatches2 = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
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
                                        .EnableBatchProcessing(3))))
                .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(HandleBatch)
                .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventTwo>>(HandleEventTwoEnumerable));

        void HandleBatch(IMessageStreamEnumerable<TestEventOne> batch)
        {
            Interlocked.Increment(ref receivedBatches1);
            List<TestEventOne> dummy = [.. batch];
        }

        void HandleEventTwoEnumerable(IMessageStreamEnumerable<TestEventTwo> batch) => Interlocked.Increment(ref receivedBatches2);

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 5; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches1 >= 2);

        receivedBatches1.ShouldBe(2);
        receivedBatches2.ShouldBe(0);
    }

    [Fact]
    public async Task Batch_ShouldNotOverlapNextSequence_WhenCompleted()
    {
        TestingCollection<List<TestEventOne>> receivedBatches = [];
        int completedBatches = 0;
        int exitedSubscribers = 0;
        bool areOverlapping = false;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
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
                                        .EnableBatchProcessing(3, TimeSpan.FromMilliseconds(300)))))
                .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch));

        async ValueTask HandleBatch(IAsyncEnumerable<TestEventOne> batch)
        {
            if (completedBatches != exitedSubscribers)
                areOverlapping = true;

            List<TestEventOne> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message);
            }

            Interlocked.Increment(ref completedBatches);

            await Task.Delay(500);

            exitedSubscribers++;
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 8; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 8);

        receivedBatches.Sum(batch => batch.Count).ShouldBe(8);
        receivedBatches.Count.ShouldBe(3);
        completedBatches.ShouldBe(2);
        exitedSubscribers.ShouldBe(2);
        areOverlapping.ShouldBeFalse();
    }

    [Fact]
    public async Task Batch_ShouldHandleBatchWithSize1()
    {
        TestingCollection<List<TestEventOne>> receivedBatches = [];
        int completedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
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
                                        .EnableBatchProcessing(1))))
                .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch));

        async ValueTask HandleBatch(IAsyncEnumerable<TestEventOne> eventsStream)
        {
            List<TestEventOne> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in eventsStream)
            {
                list.Add(message);
            }

            Interlocked.Increment(ref completedBatches);
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Count.ShouldBe(2);
        completedBatches.ShouldBe(2);
        receivedBatches[0].Count.ShouldBe(1);
        receivedBatches[1].Count.ShouldBe(1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(2);
    }

    [Fact]
    public async Task Batch_ShouldHandleExpiringBatchWithSize1()
    {
        TestingCollection<List<TestEventOne>> receivedBatches = [];
        int completedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
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
                                        .EnableBatchProcessing(10, TimeSpan.FromMilliseconds(100)))))
                .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch));

        async ValueTask HandleBatch(IAsyncEnumerable<TestEventOne> eventsStream)
        {
            List<TestEventOne> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in eventsStream)
            {
                list.Add(message);
            }

            Interlocked.Increment(ref completedBatches);
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        await producer.ProduceAsync(new TestEventOne());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Count.ShouldBe(2);
        completedBatches.ShouldBe(2);
        receivedBatches[0].Count.ShouldBe(1);
        receivedBatches[1].Count.ShouldBe(1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(2);
    }

    [Fact]
    public async Task Batch_ShouldStopConsumer_WhenMessageUnhandledMessages()
    {
        TestingCollection<List<TestEventOne>> receivedBatches = [];
        int completedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
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
                .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(HandleBatch));

        void HandleBatch(IMessageStreamEnumerable<TestEventOne> batch)
        {
            List<TestEventOne> list = [];
            receivedBatches.Add(list);

            foreach (TestEventOne message in batch)
            {
                list.Add(message);
            }

            Interlocked.Increment(ref completedBatches);
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventTwo { ContentEventTwo = "Unhandled message" });
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Count.ShouldBe(1);
        receivedBatches[0].Count.ShouldBe(1);

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Fact]
    public async Task Batch_ShouldIgnoreUnhandledMessages()
    {
        TestingCollection<List<TestEventOne>> receivedBatches = [];
        int completedBatches = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
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
                                        .EnableBatchProcessing(2)
                                        .IgnoreUnhandledMessages())))
                .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(HandleBatch));

        void HandleBatch(IMessageStreamEnumerable<TestEventOne> batch)
        {
            List<TestEventOne> list = [];
            receivedBatches.Add(list);

            foreach (TestEventOne message in batch)
            {
                list.Add(message);
            }

            Interlocked.Increment(ref completedBatches);
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventTwo { ContentEventTwo = "Unhandled message" });
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventTwo { ContentEventTwo = "Unhandled message" });
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Count.ShouldBe(2);
        receivedBatches[0].Count.ShouldBe(2);
        receivedBatches[1].Count.ShouldBe(2);
        completedBatches.ShouldBe(2);
    }

    [Fact]
    public async Task Batch_ShouldUpdateActivity_WhenSubscribingToAsyncEnumerable()
    {
        List<string> expectedTraceIds = [];
        List<string> actualTraceIds = [];
        List<string> batchStartTraceIds = [];
        List<string> batchEndTraceIds = [];
        List<string> sequenceIds = [];

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
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
                                        .EnableBatchProcessing(5))))
                .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatch));

        async ValueTask HandleBatch(IAsyncEnumerable<TestEventOne> batch)
        {
            if (Activity.Current != null)
                batchStartTraceIds.Add(Activity.Current.TraceId.ToString());

            await foreach (TestEventOne unused in batch)
            {
                if (Activity.Current != null)
                {
                    actualTraceIds.Add(Activity.Current.TraceId.ToString());
                    sequenceIds.Add(Activity.Current.Tags.SingleOrDefault(pair => pair.Key == ActivityTagNames.SequenceId).Value ?? string.Empty);
                }
            }

            if (Activity.Current != null)
                batchEndTraceIds.Add(Activity.Current.TraceId.ToString());
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 10; i++)
        {
            using Activity activity = new("TestActivity");
            activity.Start();
            expectedTraceIds.Add(activity.TraceId.ToString());

            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        actualTraceIds.ShouldBe(expectedTraceIds);
        batchStartTraceIds.ShouldBe([expectedTraceIds[0], expectedTraceIds[5]]);
        batchEndTraceIds.ShouldBe([expectedTraceIds[4], expectedTraceIds[9]]);
        sequenceIds.ShouldAllBe(sequenceId => !string.IsNullOrWhiteSpace(sequenceId));
        sequenceIds.Take(5).ShouldAllBe(sequenceId => sequenceId == sequenceIds[0]);
        sequenceIds.Skip(5).ShouldAllBe(sequenceId => sequenceId == sequenceIds[5]);
    }
}
