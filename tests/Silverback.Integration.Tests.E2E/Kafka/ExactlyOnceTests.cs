// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Database;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    public class ExactlyOnceTests : KafkaTestFixture
    {
        public ExactlyOnceTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task ExactlyOnce_InMemoryInboundLog_DuplicatedMessagesIgnored()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka(mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1))
                                .AddInMemoryInboundLog())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnsureExactlyOnce(strategy => strategy.LogMessages())
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(
                new TestEventWithUniqueKey
                {
                    UniqueKey = "1"
                });
            await publisher.PublishAsync(
                new TestEventWithUniqueKey
                {
                    UniqueKey = "2"
                });
            await publisher.PublishAsync(
                new TestEventWithUniqueKey
                {
                    UniqueKey = "1"
                });
            await publisher.PublishAsync(
                new TestEventWithUniqueKey
                {
                    UniqueKey = "2"
                });
            await publisher.PublishAsync(new TestEventOne());

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(4);

            Helper.Spy.InboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
            Helper.Spy.InboundEnvelopes[1].Message.Should().BeOfType<TestEventWithUniqueKey>();
            Helper.Spy.InboundEnvelopes[1].Message.As<TestEventWithUniqueKey>().UniqueKey.Should().Be("1");
            Helper.Spy.InboundEnvelopes[2].Message.Should().BeOfType<TestEventWithUniqueKey>();
            Helper.Spy.InboundEnvelopes[2].Message.As<TestEventWithUniqueKey>().UniqueKey.Should().Be("2");
            Helper.Spy.InboundEnvelopes[3].Message.Should().BeOfType<TestEventOne>();

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(6);
        }

        [Fact]
        public async Task ExactlyOnce_DbInboundLog_DuplicatedMessagesIgnored()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .UseDbContext<TestDbContext>()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka(mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1))
                                .AddInboundLogDatabaseTable())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnsureExactlyOnce(strategy => strategy.LogMessages())
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.EnableAutoCommit = false;
                                                config.CommitOffsetEach = 1;
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .WithTestDbContext()
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            var dbContext = Host.ScopedServiceProvider.GetRequiredService<TestDbContext>();

            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(
                new TestEventWithUniqueKey
                {
                    UniqueKey = "1"
                });
            await publisher.PublishAsync(
                new TestEventWithUniqueKey
                {
                    UniqueKey = "2"
                });
            await publisher.PublishAsync(
                new TestEventWithUniqueKey
                {
                    UniqueKey = "1"
                });
            await publisher.PublishAsync(
                new TestEventWithUniqueKey
                {
                    UniqueKey = "2"
                });
            await publisher.PublishAsync(new TestEventOne());

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(4);

            Helper.Spy.InboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
            Helper.Spy.InboundEnvelopes[1].Message.Should().BeOfType<TestEventWithUniqueKey>();
            Helper.Spy.InboundEnvelopes[1].Message.As<TestEventWithUniqueKey>().UniqueKey.Should().Be("1");
            Helper.Spy.InboundEnvelopes[2].Message.Should().BeOfType<TestEventWithUniqueKey>();
            Helper.Spy.InboundEnvelopes[2].Message.As<TestEventWithUniqueKey>().UniqueKey.Should().Be("2");
            Helper.Spy.InboundEnvelopes[3].Message.Should().BeOfType<TestEventOne>();

            dbContext.InboundMessages.Should().HaveCount(4);
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(6);
        }

        [Fact]
        public async Task ExactlyOnce_InMemoryOffsetStore_DuplicatedMessagesIgnored()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka(mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1))
                                .AddInMemoryOffsetStore())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnsureExactlyOnce(strategy => strategy.StoreOffsets())
                                        .OnPartitionsAssigned(
                                            (partitions, _) =>
                                                partitions.Select(
                                                    topicPartition => new TopicPartitionOffset(
                                                        topicPartition,
                                                        Offset.Beginning)))
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.AutoCommitIntervalMs = 50;
                                            })))
                        .AddIntegrationSpyAndSubscriber())
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
            Helper.Spy.InboundEnvelopes.Should().HaveCount(3);

            await Helper.Broker.DisconnectAsync();
            await Helper.Broker.ConnectAsync();

            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 4"
                });

            await Helper.WaitUntilAllMessagesAreConsumedAsync();
            Helper.Spy.InboundEnvelopes.Should().HaveCount(4);
        }

        [Fact]
        public async Task ExactlyOnce_DbOffsetStore_DuplicatedMessagesIgnored()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .UseDbContext<TestDbContext>()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka(mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1))
                                .AddOffsetStoreDatabaseTable())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(config => { config.BootstrapServers = "PLAINTEXT://e2e"; })
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .EnsureExactlyOnce(strategy => strategy.StoreOffsets())
                                        .OnPartitionsAssigned(
                                            (partitions, _) =>
                                                partitions.Select(
                                                    topicPartition => new TopicPartitionOffset(
                                                        topicPartition,
                                                        Offset.Beginning)))
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                                config.AutoCommitIntervalMs = 50;
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .WithTestDbContext()
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
            Helper.Spy.InboundEnvelopes.Should().HaveCount(3);

            await Helper.Broker.DisconnectAsync();
            await Helper.Broker.ConnectAsync();

            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 4"
                });

            await Helper.WaitUntilAllMessagesAreConsumedAsync();
            Helper.Spy.InboundEnvelopes.Should().HaveCount(4);
        }
    }
}
