// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class StreamingTests : KafkaTestFixture
{
    public StreamingTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Streaming_UnboundedAsyncEnumerable_MessagesReceivedAndCommitted()
    {
        List<TestEventOne> receivedMessages = new();

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
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddDelegateSubscriber(
                        async (IAsyncEnumerable<TestEventOne> eventsStream) =>
                        {
                            await foreach (TestEventOne message in eventsStream)
                            {
                                DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName)
                                    .Should().Be(receivedMessages.Count);

                                receivedMessages.Add(message);
                            }
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 15; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.Should().HaveCount(15);
        receivedMessages.Select(message => message.Content)
            .Should().BeEquivalentTo(Enumerable.Range(1, 15).Select(i => $"{i}"));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(15);
    }

    [Fact]
    public async Task Streaming_UnboundedEnumerable_MessagesReceivedAndCommitted()
    {
        List<TestEventOne> receivedMessages = new();

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
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddDelegateSubscriber(
                        (IEnumerable<TestEventOne> eventsStream) =>
                        {
                            foreach (TestEventOne message in eventsStream)
                            {
                                DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName)
                                    .Should().Be(receivedMessages.Count);

                                receivedMessages.Add(message);
                            }
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 15; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.Should().HaveCount(15);
        receivedMessages.Select(message => message.Content)
            .Should().BeEquivalentTo(Enumerable.Range(1, 15).Select(i => $"{i}"));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(15);
    }

    [Fact]
    public async Task Streaming_UnboundedStream_MessagesReceivedAndCommitted()
    {
        List<TestEventOne> receivedMessages = new();

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
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddDelegateSubscriber(
                        async (IMessageStreamEnumerable<TestEventOne> eventsStream) =>
                        {
                            await foreach (TestEventOne message in eventsStream)
                            {
                                DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName)
                                    .Should().Be(receivedMessages.Count);

                                receivedMessages.Add(message);
                            }
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 15; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.Should().HaveCount(15);
        receivedMessages.Select(message => message.Content)
            .Should().BeEquivalentTo(Enumerable.Range(1, 15).Select(i => $"{i}"));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(15);
    }

    [Fact]
    public async Task Streaming_UnboundedObservable_MessagesReceived()
    {
        List<TestEventOne> receivedMessages = new();

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .AsObservable()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddDelegateSubscriber(
                        (IMessageStreamObservable<TestEventOne> observable) =>
                            observable.Subscribe(
                                message =>
                                {
                                    DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName)
                                        .Should().Be(receivedMessages.Count);

                                    receivedMessages.Add(message);
                                })))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 3; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.Should().HaveCount(3);
        receivedMessages.Select(message => message.Content)
            .Should().BeEquivalentTo(Enumerable.Range(1, 3).Select(i => $"{i}"));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(3);
    }

    [Fact]
    public async Task Streaming_DisconnectWhileEnumerating_EnumerationAborted()
    {
        bool aborted = false;
        List<TestEventOne> receivedMessages = new();

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
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddDelegateSubscriber(
                        (IEnumerable<TestEventOne> eventsStream) =>
                        {
                            try
                            {
                                foreach (TestEventOne message in eventsStream)
                                {
                                    receivedMessages.Add(message);
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                Task.Delay(300).Wait();
                                aborted = true;
                            }
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { Content = "Message 1" });
        await publisher.PublishAsync(new TestEventOne { Content = "Message 2" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.Should().HaveCount(2);

        await Helper.Broker.DisconnectAsync();

        aborted.Should().BeTrue();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(2);
    }

    [Fact]
    public async Task Streaming_DisconnectWhileObserving_ObserverCompleted()
    {
        bool completed = false;
        ConcurrentBag<TestEventOne> receivedMessages = new();

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .AsObservable()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddDelegateSubscriber(
                        (IMessageStreamObservable<TestEventOne> observable) =>
                            observable.Subscribe(
                                message => receivedMessages.Add(message),
                                () => completed = true)))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { Content = "Message 1" });
        await publisher.PublishAsync(new TestEventOne { Content = "Message 2" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        await AsyncTestingUtil.WaitAsync(() => receivedMessages.Count >= 2);

        receivedMessages.Should().HaveCount(2);

        await Helper.Broker.DisconnectAsync();

        completed.Should().BeTrue();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(2);
    }

    [Fact]
    public async Task Streaming_UnboundedEnumerableProcessingFailed_ConsumerStopped()
    {
        List<TestEventOne> receivedMessages = new();

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .AsObservable()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddDelegateSubscriber(
                        async (IAsyncEnumerable<TestEventOne> enumerable) =>
                        {
                            await foreach (TestEventOne message in enumerable)
                            {
                                receivedMessages.Add(message);
                                if (receivedMessages.Count == 2)
                                    throw new InvalidOperationException("Test");
                            }
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { Content = "Message 1" });
        await publisher.PublishAsync(new TestEventOne { Content = "Message 2" });
        await publisher.PublishAsync(new TestEventOne { Content = "Message 3" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        await AsyncTestingUtil.WaitAsync(() => receivedMessages.Count >= 2);

        receivedMessages.Should().HaveCount(2);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(1);
        Helper.Broker.Consumers[0].IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task Streaming_UnboundedObservableProcessingFailed_ConsumerStopped()
    {
        List<TestEventOne> receivedMessages = new();

        Host.ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .AsObservable()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddDelegateSubscriber(
                        (IMessageStreamObservable<TestEventOne> observable) =>
                            observable.Subscribe(
                                message =>
                                {
                                    receivedMessages.Add(message);

                                    if (receivedMessages.Count == 2)
                                        throw new InvalidOperationException("Test");
                                })))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { Content = "Message 1" });
        await publisher.PublishAsync(new TestEventOne { Content = "Message 2" });
        await publisher.PublishAsync(new TestEventOne { Content = "Message 3" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        await AsyncTestingUtil.WaitAsync(() => receivedMessages.Count >= 2);

        receivedMessages.Should().HaveCount(2);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(1);
        Helper.Broker.Consumers[0].IsConnected.Should().BeFalse();
    }

    [Fact]
    public async Task Streaming_ProcessingPartitionsIndependently_PublishedStreamPerPartition()
    {
        ConcurrentBag<TestEventOne> receivedMessages = new();
        ConcurrentBag<IMessageStreamEnumerable<TestEventOne>> receivedStreams = new();

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
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<TestEventOne>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .WithKafkaKey(message => message?.Content))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddDelegateSubscriber(
                        async (IMessageStreamEnumerable<TestEventOne> eventsStream) =>
                        {
                            receivedStreams.Add(eventsStream);
                            await foreach (TestEventOne message in eventsStream)
                            {
                                receivedMessages.Add(message);
                            }
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 15; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedStreams.Should().HaveCount(3);
        receivedMessages.Should().HaveCount(15);
        receivedMessages.Select(message => message.Content)
            .Should().BeEquivalentTo(Enumerable.Range(1, 15).Select(i => $"{i}"));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(15);
    }

    [Fact]
    public async Task Streaming_NotProcessingPartitionsIndependently_PublishedSingleStream()
    {
        ConcurrentBag<TestEventOne> receivedMessages = new();
        ConcurrentBag<IMessageStreamEnumerable<TestEventOne>> receivedStreams = new();

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
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<TestEventOne>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .WithKafkaKey(message => message?.Content))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ProcessAllPartitionsTogether()
                                    .ConfigureClient(
                                        configuration => configuration
                                            .WithGroupId(DefaultConsumerGroupId)
                                            .CommitOffsetEach(1))))
                    .AddDelegateSubscriber(
                        async (IMessageStreamEnumerable<TestEventOne> eventsStream) =>
                        {
                            receivedStreams.Add(eventsStream);
                            await foreach (TestEventOne message in eventsStream)
                            {
                                receivedMessages.Add(message);
                            }
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 15; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedStreams.Should().HaveCount(1);
        receivedMessages.Should().HaveCount(15);
        receivedMessages.Select(message => message.Content)
            .Should().BeEquivalentTo(Enumerable.Range(1, 15).Select(i => $"{i}"));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).Should().Be(15);
    }

    [Fact]
    public async Task Streaming_FromMultiplePartitionsWithLimitedParallelism_ConcurrencyLimited()
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
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<TestEventWithKafkaKey>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .WithKafkaKey(message => message?.Content))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .LimitParallelism(2)
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddDelegateSubscriber(
                        async (IAsyncEnumerable<TestEventWithKafkaKey> eventsStream) =>
                        {
                            await foreach (TestEventWithKafkaKey message in eventsStream)
                            {
                                lock (receivedMessages)
                                {
                                    receivedMessages.Add(message);
                                }

                                await taskCompletionSource.Task;
                            }
                        }))
            .Run();

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 3; i++)
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

        receivedMessages.Should().HaveCount(12);
    }
}
