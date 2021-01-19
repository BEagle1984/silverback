// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    public class BatchTests : KafkaTestFixture
    {
        public BatchTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task Batch_SubscribingToStream_MessagesReceivedAndCommittedInBatch()
        {
            var receivedBatches = new List<List<TestEventOne>>();
            var completedBatches = 0;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })
                                        .EnableBatchProcessing(10)))
                        .AddDelegateSubscriber(
                            async (IMessageStreamEnumerable<TestEventOne> eventsStream) =>
                            {
                                var list = new List<TestEventOne>();
                                receivedBatches.Add(list);

                                await foreach (var message in eventsStream)
                                {
                                    list.Add(message);
                                }

                                completedBatches++;
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 15; i++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
            }

            await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15);

            receivedBatches.Should().HaveCount(2);
            receivedBatches[0].Should().HaveCount(10);
            receivedBatches[1].Should().HaveCount(5);
            completedBatches.Should().Be(1);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(10);

            for (int i = 16; i <= 20; i++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
            }

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            receivedBatches.Should().HaveCount(2);
            receivedBatches[0].Should().HaveCount(10);
            receivedBatches[1].Should().HaveCount(10);
            completedBatches.Should().Be(2);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(20);
        }

        [Fact]
        public async Task Batch_StreamEnumerationAborted_CommittedAndNextMessageConsumed()
        {
            var receivedBatches = 0;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })
                                        .EnableBatchProcessing(10)))
                        .AddDelegateSubscriber(
                            (IMessageStreamEnumerable<TestEventOne> _) => { receivedBatches++; }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 15; i++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
            }

            await DefaultTopic.WaitUntilAllMessagesAreConsumedAsync();

            receivedBatches.Should().Be(15);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(15);
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
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })
                                        .EnableBatchProcessing(3)))
                        .AddDelegateSubscriber(
                            (IMessageStreamEnumerable<TestEventOne> eventsStream) =>
                            {
                                receivedBatches1++;
                                var dummy = eventsStream.ToList();
                            })
                        .AddDelegateSubscriber(
                            (IMessageStreamEnumerable<TestEventTwo> _) => receivedBatches2++))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

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
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })
                                        .EnableBatchProcessing(3)))
                        .AddDelegateSubscriber(
                            (IMessageStreamEnumerable<TestEventOne> _) => receivedBatches1++)
                        .AddDelegateSubscriber(
                            (IMessageStreamEnumerable<TestEventTwo> _) => receivedBatches2++))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 5; i++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
            }

            await AsyncTestingUtil.WaitAsync(() => receivedBatches1 >= 2);

            receivedBatches1.Should().BeGreaterOrEqualTo(2);
            receivedBatches2.Should().Be(0);
        }

        [Fact]
        public async Task Batch_SubscribingToEnumerable_MessagesReceivedAndCommittedInBatch()
        {
            var receivedBatches = new List<List<TestEventOne>>();
            var completedBatches = 0;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })
                                        .EnableBatchProcessing(10)))
                        .AddDelegateSubscriber(
                            (IEnumerable<TestEventOne> eventsStream) =>
                            {
                                var list = new List<TestEventOne>();
                                receivedBatches.Add(list);

                                foreach (var message in eventsStream)
                                {
                                    list.Add(message);
                                }

                                completedBatches++;
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 15; i++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
            }

            await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15);

            receivedBatches.Should().HaveCount(2);
            receivedBatches[0].Should().HaveCount(10);
            receivedBatches[1].Should().HaveCount(5);
            completedBatches.Should().Be(1);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(10);

            for (int i = 16; i <= 20; i++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
            }

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            receivedBatches.Should().HaveCount(2);
            receivedBatches[0].Should().HaveCount(10);
            receivedBatches[1].Should().HaveCount(10);
            completedBatches.Should().Be(2);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(20);
        }

        [Fact]
        public async Task Batch_WithTimeout_IncompleteBatchCompletedAfterTimeout()
        {
            var receivedBatches = new List<List<TestEventOne>>();
            var completedBatches = 0;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })
                                        .EnableBatchProcessing(10, TimeSpan.FromMilliseconds(500))))
                        .AddDelegateSubscriber(
                            async (IMessageStreamEnumerable<TestEventOne> eventsStream) =>
                            {
                                var list = new List<TestEventOne>();
                                receivedBatches.Add(list);

                                await foreach (var message in eventsStream)
                                {
                                    list.Add(message);
                                }

                                completedBatches++;
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 15; i++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
            }

            await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15);

            receivedBatches.Should().HaveCount(2);
            receivedBatches[0].Should().HaveCount(10);
            receivedBatches[1].Should().HaveCount(5);
            completedBatches.Should().Be(1);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(10);

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            receivedBatches.Should().HaveCount(2);
            receivedBatches[0].Should().HaveCount(10);
            receivedBatches[1].Should().HaveCount(5);
            completedBatches.Should().Be(2);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(15);
        }

        [Fact]
        public async Task Batch_DisconnectWhileEnumerating_EnumerationAborted()
        {
            bool aborted = false;
            var receivedMessages = new List<TestEventOne>();

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(3)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.AutoCommitIntervalMs = 50;
                                            })
                                        .EnableBatchProcessing(10)))
                        .AddDelegateSubscriber(
                            async (IMessageStreamEnumerable<TestEventOne> eventsStream) =>
                            {
                                try
                                {
                                    await foreach (var message in eventsStream)
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

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 10; i++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
            }

            await AsyncTestingUtil.WaitAsync(() => receivedMessages.Count > 3);

            receivedMessages.Should().HaveCountGreaterThan(3);

            await Helper.Broker.DisconnectAsync();

            await AsyncTestingUtil.WaitAsync(() => aborted);

            aborted.Should().BeTrue();
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(0);
        }

        [Fact]
        public async Task Batch_ProcessingFailed_ConsumerStopped()
        {
            var receivedMessages = new List<TestEventOne>();

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })
                                        .EnableBatchProcessing(3)))
                        .AddDelegateSubscriber(
                            async (IMessageStreamEnumerable<TestEventOne> eventsStream) =>
                            {
                                await foreach (var message in eventsStream)
                                {
                                    receivedMessages.Add(message);

                                    if (receivedMessages.Count == 2)
                                        throw new InvalidOperationException("Test");
                                }
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
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

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            receivedMessages.Should().HaveCount(2);
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(0);
            Helper.Broker.Consumers[0].IsConnected.Should().BeFalse();
        }

        [Fact]
        public async Task Batch_FromMultiplePartitions_ConcurrentlyConsumed()
        {
            var receivedBatches = new List<List<TestEventOne>>();

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(3)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })
                                        .EnableBatchProcessing(10)))
                        .AddDelegateSubscriber(
                            async (IMessageStreamEnumerable<TestEventOne> eventsStream) =>
                            {
                                var list = new List<TestEventOne>();
                                lock (receivedBatches)
                                {
                                    receivedBatches.Add(list);
                                }

                                await foreach (var message in eventsStream)
                                {
                                    list.Add(message);
                                }
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

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
            var receivedMessages = new List<TestEventWithKafkaKey>();
            var taskCompletionSource = new TaskCompletionSource<bool>();

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(5)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.AutoCommitIntervalMs = 50;
                                            })
                                        .LimitParallelism(2)
                                        .EnableBatchProcessing(2)))
                        .AddDelegateSubscriber(
                            async (IMessageStreamEnumerable<TestEventWithKafkaKey> eventsStream) =>
                            {
                                await foreach (var message in eventsStream)
                                {
                                    lock (receivedMessages)
                                    {
                                        receivedMessages.Add(message);
                                    }

                                    await taskCompletionSource.Task;
                                }
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 4; i++)
            {
                await publisher.PublishAsync(
                    new TestEventWithKafkaKey
                    {
                        KafkaKey = 1,
                        Content = $"{i}"
                    });
                await publisher.PublishAsync(
                    new TestEventWithKafkaKey
                    {
                        KafkaKey = 2,
                        Content = $"{i}"
                    });
                await publisher.PublishAsync(
                    new TestEventWithKafkaKey
                    {
                        KafkaKey = 3,
                        Content = $"{i}"
                    });
                await publisher.PublishAsync(
                    new TestEventWithKafkaKey
                    {
                        KafkaKey = 4,
                        Content = $"{i}"
                    });
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
            var receivedBatches = new List<List<TestEventOne>>();
            var completedBatches = 0;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })
                                        .EnableBatchProcessing(10)))
                        .AddDelegateSubscriber(
                            async (IMessageStreamEnumerable<IInboundEnvelope<TestEventOne>> eventsStream) =>
                            {
                                var list = new List<TestEventOne>();
                                receivedBatches.Add(list);

                                await foreach (var envelope in eventsStream)
                                {
                                    list.Add(envelope.Message!);
                                }

                                completedBatches++;
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 15; i++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
            }

            await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15);

            receivedBatches.Should().HaveCount(2);
            receivedBatches[0].Should().HaveCount(10);
            receivedBatches[1].Should().HaveCount(5);
            completedBatches.Should().Be(1);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(10);

            for (int i = 16; i <= 20; i++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
            }

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            receivedBatches.Should().HaveCount(2);
            receivedBatches[0].Should().HaveCount(10);
            receivedBatches[1].Should().HaveCount(10);
            completedBatches.Should().Be(2);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(20);
        }

        [Fact]
        public async Task BatchOfBinaryFiles_SubscribingToStream_MessagesReceivedAndCommittedInBatch()
        {
            var receivedBatches = new List<List<IBinaryFileMessage>>();
            var completedBatches = 0;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IBinaryFileMessage>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .ProduceBinaryFiles())
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })
                                        .EnableBatchProcessing(10)
                                        .ConsumeBinaryFiles()))
                        .AddDelegateSubscriber(
                            async (IMessageStreamEnumerable<IBinaryFileMessage> eventsStream) =>
                            {
                                var list = new List<IBinaryFileMessage>();
                                receivedBatches.Add(list);

                                await foreach (var message in eventsStream)
                                {
                                    list.Add(message!);
                                }

                                completedBatches++;
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

            for (int i = 1; i <= 15; i++)
            {
                await publisher.PublishAsync(
                    new BinaryFileMessage
                    {
                        Content = new MemoryStream(new byte[] { 0x01, 0x02, 0x03 }),
                        ContentType = "application/pdf"
                    });
            }

            await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15);

            receivedBatches.Should().HaveCount(2);
            receivedBatches[0].Should().HaveCount(10);
            receivedBatches[1].Should().HaveCount(5);
            completedBatches.Should().Be(1);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(10);

            for (int i = 16; i <= 20; i++)
            {
                await publisher.PublishAsync(
                    new BinaryFileMessage
                    {
                        Content = new MemoryStream(new byte[] { 0x01, 0x02, 0x03 }),
                        ContentType = "application/pdf"
                    });
            }

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            receivedBatches.Should().HaveCount(2);
            receivedBatches[0].Should().HaveCount(10);
            receivedBatches[1].Should().HaveCount(10);
            completedBatches.Should().Be(2);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(20);
        }

        [Fact]
        public async Task BatchOfBinaryFiles_SubscribingToEnvelopesStream_MessagesReceivedAndCommittedInBatch()
        {
            var receivedBatches = new List<List<IBinaryFileMessage>>();
            var completedBatches = 0;

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IBinaryFileMessage>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .ProduceBinaryFiles())
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })
                                        .EnableBatchProcessing(10)
                                        .ConsumeBinaryFiles()))
                        .AddDelegateSubscriber(
                            async (IMessageStreamEnumerable<IInboundEnvelope<IBinaryFileMessage>> eventsStream) =>
                            {
                                var list = new List<IBinaryFileMessage>();
                                receivedBatches.Add(list);

                                await foreach (var envelope in eventsStream)
                                {
                                    list.Add(envelope.Message!);
                                }

                                completedBatches++;
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

            for (int i = 1; i <= 15; i++)
            {
                await publisher.PublishAsync(
                    new BinaryFileMessage
                    {
                        Content = new MemoryStream(new byte[] { 0x01, 0x02, 0x03 }),
                        ContentType = "application/pdf"
                    });
            }

            await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15);

            receivedBatches.Should().HaveCount(2);
            receivedBatches[0].Should().HaveCount(10);
            receivedBatches[1].Should().HaveCount(5);
            completedBatches.Should().Be(1);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(10);

            for (int i = 16; i <= 20; i++)
            {
                await publisher.PublishAsync(
                    new BinaryFileMessage
                    {
                        Content = new MemoryStream(new byte[] { 0x01, 0x02, 0x03 }),
                        ContentType = "application/pdf"
                    });
            }

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            receivedBatches.Should().HaveCount(2);
            receivedBatches[0].Should().HaveCount(10);
            receivedBatches[1].Should().HaveCount(10);
            completedBatches.Should().Be(2);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(20);
        }
    }
}
