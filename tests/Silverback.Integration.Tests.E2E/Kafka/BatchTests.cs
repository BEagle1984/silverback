// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Batch;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    public class BatchTests : E2ETestFixture
    {
        [Fact(Skip = "Need batch events")]
        public async Task Batch_SubscribingToSingleMessage_MessagesReceivedAndCommittedInBatch()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Batch_SubscribingToEnumerable_MessagesReceivedAndCommittedInBatch()
        {
            var receivedBatches = new List<List<TestEventOne>>();
            var completedBatches = 0;

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
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
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        },
                                        Batch = new BatchSettings
                                        {
                                            Size = 10
                                        }
                                    }))
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

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();

            await Enumerable.Range(1, 15).ForEachAsync(
                i =>
                    publisher.PublishAsync(
                        new TestEventOne
                        {
                            Content = i.ToString(CultureInfo.InvariantCulture)
                        }));
            await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) >= 15);

            receivedBatches.Should().HaveCount(2);
            receivedBatches[0].Should().HaveCount(10);
            receivedBatches[1].Should().HaveCount(5);
            completedBatches.Should().Be(1);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(10);

            await Enumerable.Range(16, 5).ForEachAsync(
                i =>
                    publisher.PublishAsync(
                        new TestEventOne
                        {
                            Content = i.ToString(CultureInfo.InvariantCulture)
                        }));
            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            receivedBatches.Should().HaveCount(2);
            receivedBatches[0].Should().HaveCount(10);
            receivedBatches[1].Should().HaveCount(10);
            completedBatches.Should().Be(2);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(20);
        }

        [Fact]
        public async Task Batch_SubscribingToStream_MessagesReceivedAndCommittedInBatch()
        {
            var receivedBatches = new List<List<TestEventOne>>();
            var completedBatches = 0;

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
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
                                            EnableAutoCommit = false,
                                            CommitOffsetEach = 1
                                        },
                                        Batch = new BatchSettings
                                        {
                                            Size = 10
                                        }
                                    }))
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

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();

            await Enumerable.Range(1, 15).ForEachAsync(
                i =>
                    publisher.PublishAsync(
                        new TestEventOne
                        {
                            Content = i.ToString(CultureInfo.InvariantCulture)
                        }));
            await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 15);

            receivedBatches.Should().HaveCount(2);
            receivedBatches[0].Should().HaveCount(10);
            receivedBatches[1].Should().HaveCount(5);
            completedBatches.Should().Be(1);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(10);

            await Enumerable.Range(16, 5).ForEachAsync(
                i =>
                    publisher.PublishAsync(
                        new TestEventOne
                        {
                            Content = i.ToString(CultureInfo.InvariantCulture)
                        }));
            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            receivedBatches.Should().HaveCount(2);
            receivedBatches[0].Should().HaveCount(10);
            receivedBatches[1].Should().HaveCount(10);
            completedBatches.Should().Be(2);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(20);
        }

        [Fact(Skip = "Not yet implemented")]
        public async Task Batch_WithTimeout_IncompleteBatchCompletedAfterTimeout()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Batch_DisconnectWhileEnumerating_EnumerationAborted()
        {
            bool aborted = false;
            var receivedMessages = new List<TestEventOne>();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
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

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            receivedMessages.Should().HaveCount(2);

            Broker.Disconnect();

            aborted.Should().BeTrue();
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(2);
        }

        [Fact]
        public async Task Batch_ProcessingFailed_ConsumerStopped()
        {
            var receivedMessages = new List<TestEventOne>();

            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
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

            await TestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            receivedMessages.Should().HaveCount(2);
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(0);
            Broker.Consumers[0].IsConnected.Should().BeFalse();
        }
    }
}
