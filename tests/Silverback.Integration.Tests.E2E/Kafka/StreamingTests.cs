// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    public class StreamingTests : E2ETestFixture
    {
        public StreamingTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task Streaming_UnboundedEnumerable_MessagesReceivedAndCommitted()
        {
            var receivedMessages = new List<TestEventOne>();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        }
                                    }))
                        .AddDelegateSubscriber(
                            async (IMessageStreamEnumerable<TestEventOne> eventsStream) =>
                            {
                                await foreach (var message in eventsStream)
                                {
                                    DefaultTopic.GetCommittedOffsetsCount("consumer1")
                                        .Should().Be(receivedMessages.Count);

                                    receivedMessages.Add(message);
                                }
                            }))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 15; i++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
            }

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            receivedMessages.Should().HaveCount(15);
            receivedMessages.Select(message => message.Content)
                .Should().BeEquivalentTo(Enumerable.Range(1, 15).Select(i => $"{i}"));

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(15);
        }

        [Fact]
        public async Task Streaming_UnboundedObservable_MessagesReceived()
        {
            var receivedMessages = new List<TestEventOne>();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .AsObservable()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        }
                                    }))
                        .AddDelegateSubscriber(
                            (IMessageStreamObservable<TestEventOne> observable) =>
                                observable.Subscribe(
                                    message =>
                                    {
                                        DefaultTopic.GetCommittedOffsetsCount("consumer1")
                                            .Should().Be(receivedMessages.Count);

                                        receivedMessages.Add(message);
                                    })))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 3; i++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
            }

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            receivedMessages.Should().HaveCount(3);
            receivedMessages.Select(message => message.Content)
                .Should().BeEquivalentTo(Enumerable.Range(1, 3).Select(i => $"{i}"));

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(3);
        }

        [Fact]
        public async Task Streaming_DisconnectWhileEnumerating_EnumerationAborted()
        {
            bool aborted = false;
            var receivedMessages = new List<TestEventOne>();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        }
                                    }))
                        .AddDelegateSubscriber(
                            (IMessageStreamEnumerable<TestEventOne> eventsStream) =>
                            {
                                try
                                {
                                    foreach (var message in eventsStream)
                                    {
                                        receivedMessages.Add(message);
                                    }
                                }
                                catch (OperationCanceledException)
                                {
                                    aborted = true;
                                }
                            }))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 1"
                });
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 2"
                });

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            receivedMessages.Should().HaveCount(2);

            Broker.Disconnect();

            aborted.Should().BeTrue();
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(2);
        }

        [Fact]
        public async Task Streaming_DisconnectWhileObserving_ObserverCompleted()
        {
            bool completed = false;
            var receivedMessages = new ConcurrentBag<TestEventOne>();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .AsObservable()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        }
                                    }))
                        .AddDelegateSubscriber(
                            (IMessageStreamObservable<TestEventOne> observable) =>
                                observable.Subscribe(
                                    message => receivedMessages.Add(message),
                                    () => completed = true)))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 1"
                });
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 2"
                });

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();
            await AsyncTestingUtil.WaitAsync(() => receivedMessages.Count >= 2);

            receivedMessages.Should().HaveCount(2);

            Broker.Disconnect();

            completed.Should().BeTrue();
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(2);
        }

        [Fact]
        public async Task Streaming_UnboundedEnumerableProcessingFailed_ConsumerStopped()
        {
            var receivedMessages = new List<TestEventOne>();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .AsObservable()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        }
                                    }))
                        .AddDelegateSubscriber(
                            async (IMessageStreamEnumerable<TestEventOne> enumerable) =>
                            {
                                await foreach (var message in enumerable)
                                {
                                    receivedMessages.Add(message);
                                    if (receivedMessages.Count == 2)
                                        throw new InvalidOperationException("Test");
                                }
                            }))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 1"
                });
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 2"
                });
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 3"
                });

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();
            await AsyncTestingUtil.WaitAsync(() => receivedMessages.Count >= 2);

            receivedMessages.Should().HaveCount(2);
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(1);
            Broker.Consumers[0].IsConnected.Should().BeFalse();
        }

        [Fact]
        public async Task Streaming_UnboundedObservableProcessingFailed_ConsumerStopped()
        {
            var receivedMessages = new List<TestEventOne>();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .AsObservable()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        }
                                    }))
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

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 1"
                });
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 2"
                });
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 3"
                });

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();
            await AsyncTestingUtil.WaitAsync(() => receivedMessages.Count >= 2);

            receivedMessages.Should().HaveCount(2);
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(1);
            Broker.Consumers[0].IsConnected.Should().BeFalse();
        }

        [Fact]
        public async Task Streaming_ProcessingPartitionsIndependently_PublishedStreamPerPartition()
        {
            var receivedMessages = new ConcurrentBag<TestEventOne>();
            var receivedStreams = new ConcurrentBag<IMessageStreamEnumerable<TestEventOne>>();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(3)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        }
                                    }))
                        .AddDelegateSubscriber(
                            async (IMessageStreamEnumerable<TestEventOne> eventsStream) =>
                            {
                                receivedStreams.Add(eventsStream);
                                await foreach (var message in eventsStream)
                                {
                                    receivedMessages.Add(message);
                                }
                            }))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 15; i++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
            }

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            receivedStreams.Should().HaveCount(3);
            receivedMessages.Should().HaveCount(15);
            receivedMessages.Select(message => message.Content)
                .Should().BeEquivalentTo(Enumerable.Range(1, 15).Select(i => $"{i}"));

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(15);
        }

        [Fact]
        public async Task Streaming_NotProcessingPartitionsIndependently_PublishedSingleStream()
        {
            var receivedMessages = new ConcurrentBag<TestEventOne>();
            var receivedStreams = new ConcurrentBag<IMessageStreamEnumerable<TestEventOne>>();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(3)))
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        },
                                        ProcessPartitionsIndependently = false
                                    }))
                        .AddDelegateSubscriber(
                            async (IMessageStreamEnumerable<TestEventOne> eventsStream) =>
                            {
                                receivedStreams.Add(eventsStream);
                                await foreach (var message in eventsStream)
                                {
                                    receivedMessages.Add(message);
                                }
                            }))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 15; i++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
            }

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            receivedStreams.Should().HaveCount(1);
            receivedMessages.Should().HaveCount(15);
            receivedMessages.Select(message => message.Content)
                .Should().BeEquivalentTo(Enumerable.Range(1, 15).Select(i => $"{i}"));

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(15);
        }
    }
}
