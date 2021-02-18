// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    public class RebalanceTests : KafkaTestFixture
    {
        public RebalanceTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task Rebalance_DefaultSettings_ProducedAndConsumedAfterRebalance()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
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
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            for (int i = 1; i <= 5; i++)
            {
                await publisher.PublishAsync(
                    new TestEventOne
                    {
                        Content = $"{i}"
                    });
            }

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(5);
            Helper.Spy.InboundEnvelopes.Should().HaveCount(5);

            DefaultTopic.Rebalance();

            for (int i = 1; i <= 5; i++)
            {
                await publisher.PublishAsync(
                    new TestEventOne
                    {
                        Content = $"{i}"
                    });
            }

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(10);
            Helper.Spy.InboundEnvelopes.Should().HaveCount(10);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(10);
        }

        [Fact]
        public async Task Rebalance_WithoutAutoCommit_PendingOffsetsCommitted()
        {
            int receivedMessages = 0;
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
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
                                                config.CommitOffsetEach = 10;
                                            })))
                        .AddDelegateSubscriber((TestEventOne _) => receivedMessages++))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "one"
                });
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "two"
                });
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "three"
                });

            await AsyncTestingUtil.WaitAsync(() => receivedMessages == 3);

            DefaultTopic.Rebalance();

            await AsyncTestingUtil.WaitAsync(() => DefaultTopic.GetCommittedOffsetsCount("consumer1") == 3);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(3);
        }

        [Fact]
        public async Task Rebalance_WithPendingBatch_AbortedAndConsumedAfterRebalance()
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
                            async (IAsyncEnumerable<TestEventOne> eventsStream) =>
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
            receivedBatches.Sum(batch => batch.Count).Should().Be(15);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(10);

            DefaultTopic.Rebalance();

            await AsyncTestingUtil.WaitAsync(() => receivedBatches.Sum(batch => batch.Count) == 20);

            receivedBatches.Should().HaveCount(3);
            receivedBatches[0].Should().HaveCount(10);
            receivedBatches[1].Should().HaveCount(5);
            receivedBatches[2].Should().HaveCount(5);
            completedBatches.Should().Be(1);
            receivedBatches.Sum(batch => batch.Count).Should().Be(20);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(10);

            for (int i = 16; i <= 20; i++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
            }

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            receivedBatches.Should().HaveCount(3);
            receivedBatches[0].Should().HaveCount(10);
            receivedBatches[1].Should().HaveCount(5);
            receivedBatches[2].Should().HaveCount(10);
            completedBatches.Should().Be(2);

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(20);
        }
    }
}
