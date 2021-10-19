// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Database;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class OutboxTests : KafkaTestFixture
{
    public OutboxTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task OutboxProduceStrategy_DefaultSettings_ProducedAndConsumed()
    {
        Host
            .WithTestDbContext()
            .ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .UseDbContext<TestDbContext>()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka()
                            .AddOutboxDatabaseTable()
                            .AddOutboxWorker(TimeSpan.FromMilliseconds(100)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<IIntegrationEvent>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .ProduceToOutbox())
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                        })))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        for (int i = 0; i < 3; i++)
        {
            using IServiceScope scope = Host.ServiceProvider.CreateScope();
            IEventPublisher publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();

            for (int j = (i * 5) + 1; j <= (i + 1) * 5; j++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"{j}" });
            }

            await dbContext.SaveChangesAsync();
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(15);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(15);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventOne)envelope.Message!).Content)
            .Should().BeEquivalentTo(Enumerable.Range(1, 15).Select(i => $"{i}"));
    }

    [Fact]
    public async Task OutboxProduceStrategy_TransactionAborted_MessageNotProduced()
    {
        Host
            .WithTestDbContext()
            .ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .UseDbContext<TestDbContext>()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka()
                            .AddOutboxDatabaseTable()
                            .AddOutboxWorker(TimeSpan.FromMilliseconds(100)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<IIntegrationEvent>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .ProduceToOutbox())
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                        })))
                    .AddIntegrationSpy())
            .Run();

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            IEventPublisher publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new TestEventOne());

            /* Just avoid call to SaveChanges */
        }

        TestDbContext dbContext = Host.ScopedServiceProvider.GetRequiredService<TestDbContext>();
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes.Should().BeEmpty();

        dbContext.Outbox.Should().BeEmpty();
        DefaultTopic.MessagesCount.Should().Be(0);
    }

    [Fact]
    public async Task OutboxProduceStrategy_EndpointNameFunction_ProducedToProperEndpoint()
    {
        Host
            .WithTestDbContext()
            .ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .UseDbContext<TestDbContext>()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka()
                            .AddOutboxDatabaseTable()
                            .AddOutboxWorker(TimeSpan.FromMilliseconds(100)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<TestEventOne>(
                                producer => producer
                                    .ProduceTo(
                                        message =>
                                        {
                                            switch (message?.Content)
                                            {
                                                case "1":
                                                    return "topic1";
                                                case "2":
                                                    return "topic2";
                                                case "3":
                                                    return "topic3";
                                                default:
                                                    throw new InvalidOperationException();
                                            }
                                        })
                                    .ProduceToOutbox()))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        TestDbContext dbContext = Host.ScopedServiceProvider.GetRequiredService<TestDbContext>();
        IInMemoryTopic topic1 = Helper.GetTopic("topic1");
        IInMemoryTopic topic2 = Helper.GetTopic("topic2");
        IInMemoryTopic topic3 = Helper.GetTopic("topic3");
        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        await publisher.PublishAsync(new TestEventOne { Content = "1" });
        await publisher.PublishAsync(new TestEventOne { Content = "2" });
        await publisher.PublishAsync(new TestEventOne { Content = "3" });

        topic1.MessagesCount.Should().Be(0);
        topic2.MessagesCount.Should().Be(0);
        topic3.MessagesCount.Should().Be(0);

        await dbContext.SaveChangesAsync();

        await AsyncTestingUtil.WaitAsync(
            () =>
                topic1.MessagesCount >= 1 &&
                topic2.MessagesCount >= 1 &&
                topic3.MessagesCount >= 1);

        dbContext.Outbox.ForEach(outboxMessage => outboxMessage.EndpointRawName.Should().NotBeNullOrEmpty());

        topic1.MessagesCount.Should().Be(1);
        topic2.MessagesCount.Should().Be(1);
        topic3.MessagesCount.Should().Be(1);
    }

    [Fact]
    public async Task OutboxProduceStrategy_DynamicNamedEndpoints_ProducedAndConsumed()
    {
        Host
            .WithTestDbContext()
            .ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .UseDbContext<TestDbContext>()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka()
                            .AddOutboxDatabaseTable()
                            .AddOutboxWorker(TimeSpan.FromMilliseconds(100)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(
                                configuration =>
                                {
                                    configuration.BootstrapServers = "PLAINTEXT://e2e";
                                })
                            .AddOutbound<TestEventOne>(
                                producer => producer
                                    .ProduceTo(_ => "some-other-topic")
                                    .WithName("other-topic")
                                    .ProduceToOutbox())
                            .AddOutbound<TestEventOne>(
                                producer => producer
                                    .ProduceTo(_ => DefaultTopicName)
                                    .WithName("my-topic")
                                    .ProduceToOutbox())
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(
                                        configuration =>
                                        {
                                            configuration.GroupId = DefaultConsumerGroupId;
                                        })))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        for (int i = 0; i < 3; i++)
        {
            using IServiceScope scope = Host.ServiceProvider.CreateScope();
            IEventPublisher publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();

            for (int j = (i * 5) + 1; j <= (i + 1) * 5; j++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"{j}" });
            }

            dbContext.Outbox.Local.ForEach(message => message.EndpointRawName.Should().StartWith("dynamic-"));
            dbContext.Outbox.Local.Where(message => message.EndpointFriendlyName == "my-topic").Should().HaveCount(5);
            dbContext.Outbox.Local.Where(message => message.EndpointFriendlyName == "other-topic").Should().HaveCount(5);

            await dbContext.SaveChangesAsync();
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(30);
        Helper.Spy.OutboundEnvelopes.Where(envelope => envelope.Endpoint.Configuration.FriendlyName == "my-topic")
            .Should().HaveCount(15);
        Helper.Spy.OutboundEnvelopes.Where(envelope => envelope.Endpoint.Configuration.FriendlyName == "other-topic")
            .Should().HaveCount(15);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(15);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventOne)envelope.Message!).Content)
            .Should().BeEquivalentTo(Enumerable.Range(1, 15).Select(i => $"{i}"));
    }
}
