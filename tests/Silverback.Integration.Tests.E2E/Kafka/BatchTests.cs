// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Sequences;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class BatchTests : KafkaTestFixture
{
    public BatchTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Batch_SubscribingToStream_MessagesReceivedAndCommittedInBatch()
    {
        List<List<TestEventOne>> receivedBatches = new();
        int completedBatches = 0;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                            configuration.EnableAutoCommit = false;
                                            configuration.CommitOffsetEach = 1;
                                        })
                                    .EnableBatchProcessing(10)))
                    .AddDelegateSubscriber(
                        async (IMessageStreamEnumerable<TestEventOne> eventsStream) =>
                        {
                            List<TestEventOne> list = new();

                            receivedBatches.ThreadSafeAdd(list);

                            await foreach (TestEventOne message in eventsStream)
                            {
                                list.Add(message);
                            }

                            Interlocked.Increment(ref completedBatches);
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 15; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15);

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(5);
        completedBatches.Should().Be(1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);

        for (int i = 16; i <= 20; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(10);
        completedBatches.Should().Be(2);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(20);
    }

    [Fact]
    public async Task Batch_SubscribingToEnumerable_MessagesReceivedAndCommittedInBatch()
    {
        List<List<TestEventOne>> receivedBatches = new();
        int completedBatches = 0;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                            configuration.EnableAutoCommit = false;
                                            configuration.CommitOffsetEach = 1;
                                        })
                                    .EnableBatchProcessing(10)))
                    .AddDelegateSubscriber(
                        (IEnumerable<TestEventOne> eventsStream) =>
                        {
                            List<TestEventOne> list = new();
                            receivedBatches.ThreadSafeAdd(list);

                            foreach (TestEventOne message in eventsStream)
                            {
                                list.Add(message);
                            }

                            Interlocked.Increment(ref completedBatches);
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 15; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15);

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(5);
        completedBatches.Should().Be(1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);

        for (int i = 16; i <= 20; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(10);
        completedBatches.Should().Be(2);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(20);
    }

    [Fact]
    public async Task Batch_SubscribingToAsyncEnumerable_MessagesReceivedAndCommittedInBatch()
    {
        List<List<TestEventOne>> receivedBatches = new();
        int completedBatches = 0;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                            configuration.EnableAutoCommit = false;
                                            configuration.CommitOffsetEach = 1;
                                        })
                                    .EnableBatchProcessing(10)))
                    .AddDelegateSubscriber(
                        async (IAsyncEnumerable<TestEventOne> eventsStream) =>
                        {
                            List<TestEventOne> list = new();
                            receivedBatches.ThreadSafeAdd(list);

                            await foreach (TestEventOne message in eventsStream)
                            {
                                list.Add(message);
                            }

                            Interlocked.Increment(ref completedBatches);
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 15; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15);

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(5);
        completedBatches.Should().Be(1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);

        for (int i = 16; i <= 20; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(10);
        completedBatches.Should().Be(2);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(20);
    }

    [Fact]
    public async Task Batch_SubscribingToObservable_MessagesReceivedAndCommittedInBatch()
    {
        List<List<TestEventOne>> receivedBatches = new();
        int completedBatches = 0;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .AsObservable()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                            configuration.EnableAutoCommit = false;
                                            configuration.CommitOffsetEach = 1;
                                        })
                                    .EnableBatchProcessing(10)))
                    .AddDelegateSubscriber(
                        (IObservable<TestEventOne> eventsStream) =>
                        {
                            List<TestEventOne> list = new();
                            receivedBatches.ThreadSafeAdd(list);

                            eventsStream.Subscribe(
                                message => list.Add(message),
                                () => Interlocked.Increment(ref completedBatches));
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 15; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15);

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(5);
        completedBatches.Should().Be(1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);

        for (int i = 16; i <= 20; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
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
    public async Task Batch_AsyncSubscriberEnumeratingSynchronously_MessagesReceivedAndCommittedInBatch()
    {
        List<List<TestEventOne>> receivedBatches = new();
        int completedBatches = 0;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                            configuration.EnableAutoCommit = false;
                                            configuration.CommitOffsetEach = 1;
                                        })
                                    .EnableBatchProcessing(10)))
                    .AddDelegateSubscriber(
                        (IEnumerable<TestEventOne> eventsStream) =>
                        {
                            List<TestEventOne> list = new();
                            receivedBatches.ThreadSafeAdd(list);

                            foreach (TestEventOne message in eventsStream)
                            {
                                list.Add(message);
                            }

                            Interlocked.Increment(ref completedBatches);

                            // Return a Task, to trick the SubscribedMethodInvoker into thinking that this
                            // is an asynchronous method, while still enumerating synchronously
                            return Task.CompletedTask;
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 15; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15);

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(5);
        completedBatches.Should().Be(1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);

        for (int i = 16; i <= 20; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(10);
        completedBatches.Should().Be(2);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(20);
    }

    [Fact]
    public async Task Batch_StreamEnumerationAborted_CommittedAndNextMessageConsumed()
    {
        int receivedBatches = 0;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                            configuration.EnableAutoCommit = false;
                                            configuration.CommitOffsetEach = 1;
                                        })
                                    .EnableBatchProcessing(10)))
                    .AddDelegateSubscriber(
                        async (IAsyncEnumerable<TestEventOne> batch) =>
                        {
                            Interlocked.Increment(ref receivedBatches);

                            int count = 0;
                            await foreach (TestEventOne dummy in batch)
                            {
                                if (++count >= 3)
                                    break;
                            }
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 15; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Should().Be(5);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(15);
    }

    [Fact]
    public async Task Batch_SubscribingToStream_OnlyMatchingStreamsReceived()
    {
        int receivedBatches1 = 0;
        int receivedBatches2 = 0;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                            configuration.EnableAutoCommit = false;
                                            configuration.CommitOffsetEach = 1;
                                        })
                                    .EnableBatchProcessing(3)))
                    .AddDelegateSubscriber(
                        (IEnumerable<TestEventOne> eventsStream) =>
                        {
                            Interlocked.Increment(ref receivedBatches1);
                            List<TestEventOne> dummy = eventsStream.ToList();
                        })
                    .AddDelegateSubscriber((IEnumerable<TestEventTwo> _) => Interlocked.Increment(ref receivedBatches2)))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches1 >= 2);

        receivedBatches1.Should().Be(2);
        receivedBatches2.Should().Be(0);
    }

    [Fact]
    public async Task Batch_StreamEnumerationAbortedWithOtherNotMatchingSubscriber_EnumerationAborted()
    {
        int receivedBatches1 = 0;
        int receivedBatches2 = 0;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                            configuration.EnableAutoCommit = false;
                                            configuration.CommitOffsetEach = 1;
                                        })
                                    .EnableBatchProcessing(3)))
                    .AddDelegateSubscriber((IEnumerable<TestEventOne> _) => Interlocked.Increment(ref receivedBatches1))
                    .AddDelegateSubscriber((IEnumerable<TestEventTwo> _) => Interlocked.Increment(ref receivedBatches2)))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches1 >= 2);

        receivedBatches1.Should().BeGreaterOrEqualTo(2);
        receivedBatches2.Should().Be(0);
    }

    [Fact]
    public async Task Batch_Completed_NoOverlapOfNextSequence()
    {
        List<List<TestEventOne>> receivedBatches = new();
        int completedBatches = 0;
        int exitedSubscribers = 0;
        bool areOverlapping = false;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                            configuration.EnableAutoCommit = false;
                                            configuration.CommitOffsetEach = 1;
                                        })
                                    .EnableBatchProcessing(3, TimeSpan.FromMilliseconds(300))))
                    .AddDelegateSubscriber(
                        async (IAsyncEnumerable<TestEventOne> eventsStream) =>
                        {
                            if (completedBatches != exitedSubscribers)
                                areOverlapping = true;

                            List<TestEventOne> list = new();
                            receivedBatches.ThreadSafeAdd(list);

                            await foreach (TestEventOne message in eventsStream)
                            {
                                list.Add(message);
                            }

                            Interlocked.Increment(ref completedBatches);

                            await Task.Delay(500);

                            exitedSubscribers++;
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 8; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 8);

        receivedBatches.Should().HaveCount(3);
        completedBatches.Should().Be(2);
        exitedSubscribers.Should().Be(2);
        areOverlapping.Should().BeFalse();
    }

    [Fact]
    public async Task Batch_WithTimeout_IncompleteBatchCompletedAfterTimeout()
    {
        List<List<TestEventOne>> receivedBatches = new();
        int completedBatches = 0;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                            configuration.EnableAutoCommit = false;
                                            configuration.CommitOffsetEach = 1;
                                        })
                                    .EnableBatchProcessing(10, TimeSpan.FromMilliseconds(500))))
                    .AddDelegateSubscriber(
                        async (IAsyncEnumerable<TestEventOne> eventsStream) =>
                        {
                            List<TestEventOne> list = new();
                            receivedBatches.ThreadSafeAdd(list);

                            await foreach (TestEventOne message in eventsStream)
                            {
                                list.Add(message);
                            }

                            Interlocked.Increment(ref completedBatches);
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 15; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15);

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(5);
        completedBatches.Should().Be(1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(5);
        completedBatches.Should().Be(2);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(15);
    }

    [Fact]
    public async Task Batch_ElapsedTimeout_NoOverlapOfNextSequence()
    {
        List<List<TestEventOne>> receivedBatches = new();
        int completedBatches = 0;
        int exitedSubscribers = 0;
        bool areOverlapping = false;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                            configuration.EnableAutoCommit = false;
                                            configuration.CommitOffsetEach = 1;
                                        })
                                    .EnableBatchProcessing(10, TimeSpan.FromMilliseconds(200))))
                    .AddDelegateSubscriber(
                        async (IAsyncEnumerable<TestEventOne> eventsStream) =>
                        {
                            if (completedBatches != exitedSubscribers)
                                areOverlapping = true;

                            List<TestEventOne> list = new();
                            receivedBatches.ThreadSafeAdd(list);

                            await foreach (TestEventOne message in eventsStream)
                            {
                                list.Add(message);
                            }

                            Interlocked.Increment(ref completedBatches);

                            await Task.Delay(500);

                            Interlocked.Increment(ref exitedSubscribers);
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 5);

        receivedBatches.Should().HaveCount(1);
        receivedBatches[0].Should().HaveCount(5);
        completedBatches.Should().Be(0);

        await AsyncTestingUtil.WaitAsync(() => completedBatches == 1);

        completedBatches.Should().Be(1);
        exitedSubscribers.Should().Be(0);

        for (int i = 1; i <= 10; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        completedBatches.Should().BeGreaterThan(1);
        exitedSubscribers.Should().BeGreaterThan(1);
        areOverlapping.Should().BeFalse();
    }

    [Fact]
    public async Task Batch_DisconnectWhileEnumerating_EnumerationAborted()
    {
        int batchesCount = 0;
        int abortedCount = 0;
        List<TestEventOne> receivedMessages = new();

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<TestEventOne>(
                                endpoint => endpoint
                                    .ProduceTo(DefaultTopicName)
                                    .WithKafkaKey(message => message?.Content))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                        })
                                    .EnableBatchProcessing(10)))
                    .AddDelegateSubscriber(
                        async (IAsyncEnumerable<TestEventOne> eventsStream) =>
                        {
                            Interlocked.Increment(ref batchesCount);

                            try
                            {
                                await foreach (TestEventOne message in eventsStream)
                                {
                                    receivedMessages.ThreadSafeAdd(message);
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                // Simulate something going on in the subscribed method
                                await Task.Delay(300);

                                Interlocked.Increment(ref abortedCount);
                            }
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 10; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedMessages.Count > 3);

        receivedMessages.Should().HaveCountGreaterThan(3);

        ISequenceStoreCollection sequenceStores = Helper.Broker.Consumers[0].SequenceStores;
        List<ISequence> sequences = sequenceStores.SelectMany(store => store).ToList();

        await Helper.Broker.DisconnectAsync();

        sequences.ForEach(sequence => sequence.IsAborted.Should().BeTrue());

        await AsyncTestingUtil.WaitAsync(() => abortedCount == batchesCount);

        abortedCount.Should().Be(batchesCount);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(0);
    }

    [Fact]
    public async Task Batch_DisconnectWhileMaterializing_EnumerationAborted()
    {
        int batchesCount = 0;
        int abortedCount = 0;
        List<TestEventOne> receivedMessages = new();

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<TestEventOne>(
                                endpoint => endpoint
                                    .ProduceTo(DefaultTopicName)
                                    .WithKafkaKey(message => message?.Content))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                        })
                                    .EnableBatchProcessing(10)))
                    .AddDelegateSubscriber(
                        async (IEnumerable<TestEventOne> eventsStream) =>
                        {
                            Interlocked.Increment(ref batchesCount);

                            try
                            {
                                List<TestEventOne> messages = eventsStream.ToList();
                                messages.ForEach(receivedMessages.ThreadSafeAdd);
                            }
                            catch (OperationCanceledException)
                            {
                                // Simulate something going on in the subscribed method
                                await Task.Delay(300);

                                Interlocked.Increment(ref abortedCount);
                            }
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 10; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Task.Delay(100);

        ISequenceStoreCollection sequenceStores = Helper.Broker.Consumers[0].SequenceStores;
        List<ISequence> sequences = sequenceStores.SelectMany(store => store).ToList();

        await Helper.Broker.DisconnectAsync();

        sequences.Should().HaveCount(batchesCount);
        sequences.ForEach(sequence => sequence.IsAborted.Should().BeTrue());

        abortedCount.Should().Be(batchesCount);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(0);
    }

    [Fact]
    public async Task Batch_DisconnectAfterEnumerationCompleted_SubscriberAwaitedBeforeDisconnecting()
    {
        List<TestEventOne> receivedMessages = new();
        bool hasSubscriberReturned = false;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                            configuration.EnableAutoCommit = false;
                                            configuration.CommitOffsetEach = 1;
                                        })
                                    .EnableBatchProcessing(10)))
                    .AddDelegateSubscriber(
                        async (IAsyncEnumerable<TestEventOne> eventsStream) =>
                        {
                            await foreach (TestEventOne message in eventsStream)
                            {
                                receivedMessages.ThreadSafeAdd(message);
                            }

                            await Task.Delay(500);

                            hasSubscriberReturned = true;
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 10; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedMessages.Count >= 10);

        receivedMessages.Should().HaveCount(10);
        hasSubscriberReturned.Should().BeFalse();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(0);

        await Helper.Broker.DisconnectAsync();

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);
        hasSubscriberReturned.Should().BeTrue();
    }

    [Fact]
    public async Task Batch_ProcessingFailed_ConsumerStopped()
    {
        int receivedMessages = 0;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                            configuration.EnableAutoCommit = false;
                                            configuration.CommitOffsetEach = 1;
                                        })
                                    .EnableBatchProcessing(3)))
                    .AddDelegateSubscriber(
                        async (IAsyncEnumerable<TestEventOne> eventsStream) =>
                        {
                            await foreach (TestEventOne dummy in eventsStream)
                            {
                                Interlocked.Increment(ref receivedMessages);

                                if (receivedMessages == 2)
                                    throw new InvalidOperationException("Test");
                            }
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { Content = "Message 1" });
        await publisher.PublishAsync(new TestEventOne { Content = "Message 2" });
        await publisher.PublishAsync(new TestEventOne { Content = "Message 3" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.Should().Be(2);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(0);
        Helper.Broker.Consumers[0].IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task Batch_ProcessingFailedWithMultiplePendingBatches_ConsumerStopped()
    {
        int batchesCount = 0;
        int abortedCount = 0;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<TestEventOne>(
                                endpoint => endpoint
                                    .ProduceTo(DefaultTopicName)
                                    .WithKafkaKey(message => message?.Content))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                            configuration.EnableAutoCommit = false;
                                            configuration.CommitOffsetEach = 1;
                                        })
                                    .EnableBatchProcessing(10)))
                    .AddDelegateSubscriber(
                        async (IAsyncEnumerable<TestEventOne> eventsStream) =>
                        {
                            int batchIndex = Interlocked.Increment(ref batchesCount);

                            int messagesCount = 0;

                            try
                            {
                                await foreach (TestEventOne dummy in eventsStream)
                                {
                                    if (++messagesCount == 2 && batchIndex == 2)
                                        throw new InvalidOperationException("Test");
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                Interlocked.Increment(ref abortedCount);
                            }
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        for (int i = 0; i < 10; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        await AsyncTestingUtil.WaitAsync(() => !Helper.Broker.Consumers[0].IsConnected);

        Helper.Broker.Consumers[0].IsConnected.Should().BeFalse();
        batchesCount.Should().BeGreaterThan(1);
        abortedCount.Should().BeGreaterOrEqualTo(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(0);
    }

    [Fact]
    public async Task Batch_FromMultiplePartitions_ConcurrentlyConsumed()
    {
        List<List<TestEventOne>> receivedBatches = new();

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<TestEventOne>(
                                endpoint => endpoint
                                    .ProduceTo(DefaultTopicName)
                                    .WithKafkaKey(message => message?.Content))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                            configuration.EnableAutoCommit = false;
                                            configuration.CommitOffsetEach = 1;
                                        })
                                    .EnableBatchProcessing(10)))
                    .AddDelegateSubscriber(
                        async (IAsyncEnumerable<TestEventOne> eventsStream) =>
                        {
                            List<TestEventOne> list = new();
                            receivedBatches.ThreadSafeAdd(list);

                            await foreach (TestEventOne message in eventsStream)
                            {
                                list.Add(message);
                            }
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 10; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 10);

        receivedBatches.Count.Should().BeGreaterThan(1);
        receivedBatches.Sum(batch => batch.Count).Should().Be(10);
    }

    [Fact]
    public async Task Batch_FromMultiplePartitionsWithLimitedParallelism_ConcurrencyLimited()
    {
        List<TestEventWithKafkaKey> receivedMessages = new();
        TaskCompletionSource<bool> taskCompletionSource = new();

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
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                        })
                                    .LimitParallelism(2)
                                    .EnableBatchProcessing(2)))
                    .AddDelegateSubscriber(
                        async (IAsyncEnumerable<TestEventWithKafkaKey> eventsStream) =>
                        {
                            await foreach (TestEventWithKafkaKey message in eventsStream)
                            {
                                receivedMessages.ThreadSafeAdd(message);
                                await taskCompletionSource.Task;
                            }
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 4; i++)
        {
            await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = 1, Content = $"{i}" });
            await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = 2, Content = $"{i}" });
            await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = 3, Content = $"{i}" });
            await publisher.PublishAsync(new TestEventWithKafkaKey { KafkaKey = 4, Content = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedMessages.Count >= 2);
        await Task.Delay(100);

        try
        {
            receivedMessages.Should().HaveCount(2);
        }
        finally
        {
            taskCompletionSource.SetResult(true);
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.Should().HaveCount(16);
    }

    [Fact]
    public async Task Batch_SubscribingToEnvelopesStream_MessagesReceivedAndCommittedInBatch()
    {
        List<List<TestEventOne>> receivedBatches = new();
        int completedBatches = 0;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                            configuration.EnableAutoCommit = false;
                                            configuration.CommitOffsetEach = 1;
                                        })
                                    .EnableBatchProcessing(10)))
                    .AddDelegateSubscriber(
                        async (IAsyncEnumerable<IInboundEnvelope<TestEventOne>> eventsStream) =>
                        {
                            List<TestEventOne> list = new();
                            receivedBatches.ThreadSafeAdd(list);

                            await foreach (IInboundEnvelope<TestEventOne> envelope in eventsStream)
                            {
                                list.Add(envelope.Message!);
                            }

                            Interlocked.Increment(ref completedBatches);
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 15; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15);

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(5);
        completedBatches.Should().Be(1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);

        for (int i = 16; i <= 20; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(10);
        completedBatches.Should().Be(2);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(20);
    }

    [Fact]
    public async Task BatchOfBinaryFiles_SubscribingToStream_MessagesReceivedAndCommittedInBatch()
    {
        List<List<IBinaryFileMessage>> receivedBatches = new();
        int completedBatches = 0;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<IBinaryFileMessage>(
                                endpoint => endpoint
                                    .ProduceTo(DefaultTopicName)
                                    .ProduceBinaryFiles())
                            .AddInbound(
                                endpoint => endpoint
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                            configuration.EnableAutoCommit = false;
                                            configuration.CommitOffsetEach = 1;
                                        })
                                    .EnableBatchProcessing(10)
                                    .ConsumeBinaryFiles()))
                    .AddDelegateSubscriber(
                        async (IAsyncEnumerable<IBinaryFileMessage> eventsStream) =>
                        {
                            List<IBinaryFileMessage> list = new();
                            receivedBatches.ThreadSafeAdd(list);

                            await foreach (IBinaryFileMessage message in eventsStream)
                            {
                                list.Add(message);
                            }

                            Interlocked.Increment(ref completedBatches);
                        }))
            .Run();

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 15; i++)
        {
            await publisher.PublishAsync(
                new BinaryFileMessage
                {
                    Content = BytesUtil.GetRandomStream(),
                    ContentType = "application/pdf"
                });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15);

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(5);
        completedBatches.Should().Be(1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);

        for (int i = 16; i <= 20; i++)
        {
            await publisher.PublishAsync(
                new BinaryFileMessage
                {
                    Content = BytesUtil.GetRandomStream(),
                    ContentType = "application/pdf"
                });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(10);
        completedBatches.Should().Be(2);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(20);
    }

    [Fact]
    public async Task
        BatchOfBinaryFiles_SubscribingToEnvelopesStream_MessagesReceivedAndCommittedInBatch()
    {
        List<List<IBinaryFileMessage>> receivedBatches = new();
        int completedBatches = 0;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<IBinaryFileMessage>(
                                endpoint => endpoint
                                    .ProduceTo(DefaultTopicName)
                                    .ProduceBinaryFiles())
                            .AddInbound(
                                endpoint => endpoint
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                            configuration.EnableAutoCommit = false;
                                            configuration.CommitOffsetEach = 1;
                                        })
                                    .EnableBatchProcessing(10)
                                    .ConsumeBinaryFiles()))
                    .AddDelegateSubscriber(
                        async (IAsyncEnumerable<IInboundEnvelope<IBinaryFileMessage>> eventsStream) =>
                        {
                            List<IBinaryFileMessage> list = new();
                            receivedBatches.ThreadSafeAdd(list);

                            await foreach (IInboundEnvelope<IBinaryFileMessage> envelope in eventsStream)
                            {
                                list.Add(envelope.Message!);
                            }

                            Interlocked.Increment(ref completedBatches);
                        }))
            .Run();

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 15; i++)
        {
            await publisher.PublishAsync(
                new BinaryFileMessage
                {
                    Content = BytesUtil.GetRandomStream(),
                    ContentType = "application/pdf"
                });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15);

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(5);
        completedBatches.Should().Be(1);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(10);

        for (int i = 16; i <= 20; i++)
        {
            await publisher.PublishAsync(
                new BinaryFileMessage
                {
                    Content = BytesUtil.GetRandomStream(),
                    ContentType = "application/pdf"
                });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Should().HaveCount(2);
        receivedBatches[0].Should().HaveCount(10);
        receivedBatches[1].Should().HaveCount(10);
        completedBatches.Should().Be(2);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(20);
    }

    [Fact]
    public async Task Batch_WithSize1_MessageBatchReceived()
    {
        List<List<TestEventOne>> receivedBatches = new();
        int completedBatches = 0;

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                            configuration.EnableAutoCommit = false;
                                            configuration.CommitOffsetEach = 1;
                                        })
                                    .EnableBatchProcessing(1)))
                    .AddDelegateSubscriber(
                        async (IAsyncEnumerable<TestEventOne> eventsStream) =>
                        {
                            List<TestEventOne> list = new();
                            receivedBatches.ThreadSafeAdd(list);

                            await foreach (TestEventOne message in eventsStream)
                            {
                                list.Add(message);
                            }

                            Interlocked.Increment(ref completedBatches);
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.Should().HaveCount(3);
        completedBatches.Should().Be(3);
        receivedBatches.Sum(batch => batch.Count).Should().Be(3);

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);
    }

    [Fact]
    public async Task Batch_WithMultiplePartitions_StreamPerPartitionIsReceived()
    {
        List<List<TestEventOne>> receivedBatches = new();

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(2)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<TestEventOne>(
                                endpoint => endpoint
                                    .ProduceTo(DefaultTopicName)
                                    .WithKafkaKey(message => message?.Content))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                            configuration.EnableAutoCommit = false;
                                            configuration.CommitOffsetEach = 1;
                                        })
                                    .EnableBatchProcessing(20)))
                    .AddDelegateSubscriber(
                        async (IAsyncEnumerable<TestEventOne> eventsStream) =>
                        {
                            List<TestEventOne> list = new();
                            receivedBatches.ThreadSafeAdd(list);

                            await foreach (TestEventOne message in eventsStream)
                            {
                                list.Add(message);
                            }
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 15; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15);

        receivedBatches.Should().HaveCount(2);
        receivedBatches.Sum(batch => batch.Count).Should().Be(15);
    }

    [Fact]
    public async Task Batch_WithMultiplePartitionsProcessedTogether_SingleStreamIsReceived()
    {
        List<List<TestEventOne>> receivedBatches = new();

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(2)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<TestEventOne>(
                                endpoint => endpoint
                                    .ProduceTo(DefaultTopicName)
                                    .WithKafkaKey(message => message?.Content))
                            .AddInbound(
                                endpoint => endpoint
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                            configuration.EnableAutoCommit = false;
                                            configuration.CommitOffsetEach = 1;
                                        })
                                    .EnableBatchProcessing(20)
                                    .ProcessAllPartitionsTogether()))
                    .AddDelegateSubscriber(
                        async (IAsyncEnumerable<TestEventOne> eventsStream) =>
                        {
                            List<TestEventOne> list = new();
                            receivedBatches.ThreadSafeAdd(list);

                            await foreach (TestEventOne message in eventsStream)
                            {
                                list.Add(message);
                            }
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 15; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15);

        receivedBatches.Should().HaveCount(1);
        receivedBatches.Sum(batch => batch.Count).Should().Be(15);
    }
}
