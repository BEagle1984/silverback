// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

// TODO: Test with multiple endpoints for the same consumer
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

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(5);
        completedBatches.Should().Be(1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);

        for (int i = 16; i <= 20; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(10);
        completedBatches.Should().Be(2);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(20);
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

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(5);
        completedBatches.Should().Be(1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);

        for (int i = 16; i <= 20; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(10);
        completedBatches.Should().Be(2);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(20);
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

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(5);
        completedBatches.Should().Be(1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);

        for (int i = 16; i <= 20; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(10);
        completedBatches.Should().Be(2);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(20);
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

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(5);
        completedBatches.Should().Be(1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);

        for (int i = 16; i <= 20; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(10);
        completedBatches.Should().Be(2);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(20);
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

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(5);
        completedBatches.Should().Be(1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);

        for (int i = 16; i <= 20; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(10);
        completedBatches.Should().Be(2);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(20);
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

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(5);
        completedBatches.Should().Be(1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);

        for (int i = 16; i <= 20; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(10);
        completedBatches.Should().Be(2);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(20);
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

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(5);
        completedBatches.Should().Be(1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);

        for (int i = 16; i <= 20; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(10);
        completedBatches.Should().Be(2);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(20);
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

        receivedBatches1.Should().Be(2);
        receivedBatches2.Should().Be(0);
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

        receivedBatches.Should().HaveCount(3);
        completedBatches.Should().Be(2);
        exitedSubscribers.Should().Be(2);
        areOverlapping.Should().BeFalse();
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

        receivedBatches.Should().HaveCount(2);
        completedBatches.Should().Be(2);
        receivedBatches[0].Count.Should().Be(1);
        receivedBatches[1].Count.Should().Be(1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(2);
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

        receivedBatches.Should().HaveCount(2);
        completedBatches.Should().Be(2);
        receivedBatches[0].Count.Should().Be(1);
        receivedBatches[1].Count.Should().Be(1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(2);
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

        receivedBatches.Should().HaveCount(1);
        receivedBatches[0].Should().HaveCount(1);

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        consumer.StatusInfo.Status.Should().Be(ConsumerStatus.Stopped);
        consumer.Client.Status.Should().Be(ClientStatus.Disconnected);
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

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(2);
        receivedBatches[1].Should().HaveCount(2);
        completedBatches.Should().Be(2);
    }
}
