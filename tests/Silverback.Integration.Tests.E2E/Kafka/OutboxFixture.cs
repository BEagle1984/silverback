// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Silverback.Messaging.Publishing;
using Silverback.Storage;
using Silverback.Storage.Relational;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class OutboxFixture : KafkaTestFixture
{
    public OutboxFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task OutboxProduceStrategy_ShouldProduceMessagesViaOutboxWorker()
    {
        Host
            .ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka()
                            .AddSqliteOutbox()
                            // TODO: Replace with builder
                            .AddOutboxWorker(
                                new OutboxWorkerSettings(new SqliteOutboxSettings(Host.SqliteConnectionString))
                                {
                                    Interval = TimeSpan.FromMilliseconds(100)
                                }))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    // TODO: Replace with builder
                                    .ProduceToOutbox(new SqliteOutboxSettings(Host.SqliteConnectionString)))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        SilverbackStorageInitializer storageInitializer = Host.ScopedServiceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreateSqliteOutboxAsync(new SqliteOutboxSettings(Host.SqliteConnectionString));

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 0; i < 10; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(10);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(10);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventOne)envelope.Message!).Content)
            .Should().BeEquivalentTo(Enumerable.Range(0, 10).Select(i => $"{i}"));
    }

    [Fact]
    public async Task OutboxProduceStrategy_ShouldProduceMessages_WhenUsingMultipleOutboxes()
    {
        Host
            .ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka()
                            .AddSqliteOutbox()
                            // TODO: Replace with builder
                            .AddOutboxWorker(
                                new OutboxWorkerSettings(new SqliteOutboxSettings(Host.SqliteConnectionString, "outbox1"))
                                {
                                    Interval = TimeSpan.FromMilliseconds(100)
                                })
                            .AddOutboxWorker(
                                new OutboxWorkerSettings(new SqliteOutboxSettings(Host.SqliteConnectionString, "outbox2"))
                                {
                                    Interval = TimeSpan.FromMilliseconds(100)
                                }))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<TestEventOne>(
                                producer => producer
                                    .ProduceTo("topic1")
                                    // TODO: Replace with builder
                                    .ProduceToOutbox(new SqliteOutboxSettings(Host.SqliteConnectionString, "outbox1")))
                            .AddOutbound<TestEventTwo>(
                                producer => producer
                                    .ProduceTo("topic2")
                                    // TODO: Replace with builder
                                    .ProduceToOutbox(new SqliteOutboxSettings(Host.SqliteConnectionString, "outbox2")))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom("topic1")
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId)))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom("topic2")
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        SilverbackStorageInitializer storageInitializer = Host.ScopedServiceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreateSqliteOutboxAsync(new SqliteOutboxSettings(Host.SqliteConnectionString, "outbox1"));
        await storageInitializer.CreateSqliteOutboxAsync(new SqliteOutboxSettings(Host.SqliteConnectionString, "outbox2"));

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 0; i < 3; i++)
        {
            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(new TestEventTwo());
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(6);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(6);
        List<object?> inboundMessages = Helper.Spy.InboundEnvelopes.Select(envelope => envelope.Message).ToList();

        inboundMessages.OfType<TestEventOne>().Should().HaveCount(3);
        inboundMessages.OfType<TestEventTwo>().Should().HaveCount(3);
    }

    [Fact]
    public async Task OutboxProduceStrategy_ShouldProduceToCorrectTopic_WhenUsingEndpointNameFunction()
    {
        Host
            .ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka()
                            .AddSqliteOutbox()
                            // TODO: Replace with builder
                            .AddOutboxWorker(
                                new OutboxWorkerSettings(new SqliteOutboxSettings(Host.SqliteConnectionString))
                                {
                                    Interval = TimeSpan.FromMilliseconds(100)
                                }))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
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
                                    // TODO: Replace with builder
                                    .ProduceToOutbox(new SqliteOutboxSettings(Host.SqliteConnectionString))))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        SilverbackStorageInitializer storageInitializer = Host.ScopedServiceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreateSqliteOutboxAsync(new SqliteOutboxSettings(Host.SqliteConnectionString));

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

        await AsyncTestingUtil.WaitAsync(
            () =>
                topic1.MessagesCount >= 1 &&
                topic2.MessagesCount >= 1 &&
                topic3.MessagesCount >= 1);

        topic1.MessagesCount.Should().Be(1);
        topic2.MessagesCount.Should().Be(1);
        topic3.MessagesCount.Should().Be(1);
    }

    [Fact]
    public async Task OutboxProduceStrategy_ShouldProduceToCorrectTopic_WhenUsingDynamicNamedEndpoints()
    {
        Host
            .ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka()
                            .AddSqliteOutbox()
                            // TODO: Replace with builder
                            .AddOutboxWorker(
                                new OutboxWorkerSettings(new SqliteOutboxSettings(Host.SqliteConnectionString))
                                {
                                    Interval = TimeSpan.FromMilliseconds(100)
                                }))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<TestEventOne>(
                                producer => producer
                                    .ProduceTo(_ => "some-other-topic")
                                    .WithName("other-topic")
                                    // TODO: Replace with builder
                                    .ProduceToOutbox(new SqliteOutboxSettings(Host.SqliteConnectionString)))
                            .AddOutbound<TestEventOne>(
                                producer => producer
                                    .ProduceTo(_ => DefaultTopicName)
                                    .WithName("my-topic")
                                    // TODO: Replace with builder
                                    .ProduceToOutbox(new SqliteOutboxSettings(Host.SqliteConnectionString)))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        SilverbackStorageInitializer storageInitializer = Host.ScopedServiceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreateSqliteOutboxAsync(new SqliteOutboxSettings(Host.SqliteConnectionString));

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 0; i < 3; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(6);
        Helper.Spy.OutboundEnvelopes.Where(envelope => envelope.Endpoint.RawName == DefaultTopicName).Should().HaveCount(3);
        Helper.Spy.OutboundEnvelopes.Where(envelope => envelope.Endpoint.RawName == "some-other-topic").Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventOne)envelope.Message!).Content)
            .Should().BeEquivalentTo(Enumerable.Range(0, 3).Select(i => $"{i}"));
    }

    [Fact]
    public async Task OutboxProduceStrategy_ShouldUseTransaction()
    {
        Host
            .ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka()
                            .AddSqliteOutbox()
                            // TODO: Replace with builder
                            .AddOutboxWorker(
                                new OutboxWorkerSettings(new SqliteOutboxSettings(Host.SqliteConnectionString))
                                {
                                    Interval = TimeSpan.FromMilliseconds(100)
                                }))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    // TODO: Replace with builder
                                    .ProduceToOutbox(new SqliteOutboxSettings(Host.SqliteConnectionString)))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        SilverbackStorageInitializer storageInitializer = Host.ScopedServiceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreateSqliteOutboxAsync(new SqliteOutboxSettings(Host.SqliteConnectionString));

        await using SqliteConnection connection = new(Host.SqliteConnectionString);
        await connection.OpenAsync();

        await using (DbTransaction transaction = await connection.BeginTransactionAsync())
        {
            IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            publisher.EnlistTransaction(transaction);

            for (int i = 0; i < 3; i++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"rollback {i}" });
            }

            await transaction.RollbackAsync();
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(0);

        await using (DbTransaction transaction = await connection.BeginTransactionAsync())
        {
            IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            publisher.EnlistTransaction(transaction);

            for (int i = 0; i < 3; i++)
            {
                await publisher.PublishAsync(new TestEventOne { Content = $"commit {i}" });
            }

            await transaction.CommitAsync();
        }

        await connection.CloseAsync();

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(6);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventOne)envelope.Message!).Content)
            .Should().BeEquivalentTo(Enumerable.Range(0, 3).Select(i => $"commit {i}"));
    }

    [Fact]
    public async Task OutboxProduceStrategy_ShouldIgnoreTransaction_WhenUsingInMemoryStorage()
    {
        Host
            .ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka()
                            .UseInMemoryOutbox()
                            // TODO: Replace with builder
                            .AddOutboxWorker(
                                new OutboxWorkerSettings(new SqliteOutboxSettings(Host.SqliteConnectionString))
                                {
                                    Interval = TimeSpan.FromMilliseconds(100)
                                }))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    // TODO: Replace with builder
                                    .ProduceToOutbox(new SqliteOutboxSettings(Host.SqliteConnectionString)))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddIntegrationSpy())
            .Run();

        SilverbackStorageInitializer storageInitializer = Host.ScopedServiceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreateSqliteOutboxAsync(new SqliteOutboxSettings(Host.SqliteConnectionString));

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        await using SqliteConnection connection = new(Host.SqliteConnectionString);
        await connection.OpenAsync();
        await using DbTransaction transaction = await connection.BeginTransactionAsync();

        publisher.EnlistTransaction(transaction);
        await publisher.PublishAsync(new TestEventOne());
        await transaction.RollbackAsync();
        await connection.CloseAsync();

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        DefaultTopic.MessagesCount.Should().Be(1);
    }

    [Fact]
    public async Task OutboxProduceStrategy_ShouldProduceMessages_WhenUsingSqlite()
    {
        Host
            .ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka()
                            .AddSqliteOutbox()
                            // TODO: Replace with builder
                            .AddOutboxWorker(
                                new OutboxWorkerSettings(new SqliteOutboxSettings(Host.SqliteConnectionString))
                                {
                                    Interval = TimeSpan.FromMilliseconds(100)
                                }))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    // TODO: Replace with builder
                                    .ProduceToOutbox(new SqliteOutboxSettings(Host.SqliteConnectionString)))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        SilverbackStorageInitializer storageInitializer = Host.ScopedServiceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreateSqliteOutboxAsync(new SqliteOutboxSettings(Host.SqliteConnectionString));

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 0; i < 3; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventOne)envelope.Message!).Content)
            .Should().BeEquivalentTo(Enumerable.Range(0, 3).Select(i => $"{i}"));
    }

    [Fact]
    public async Task OutboxProduceStrategy_ShouldProduceMessages_WhenUsingInMemoryStorage()
    {
        Host
            .ConfigureServices(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka()
                            .AddInMemoryOutbox()
                            // TODO: Replace with builder
                            .AddOutboxWorker(
                                new OutboxWorkerSettings(new InMemoryOutboxSettings())
                                {
                                    Interval = TimeSpan.FromMilliseconds(100)
                                }))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    // TODO: Replace with builder
                                    .ProduceToOutbox(new InMemoryOutboxSettings()))
                            .AddInbound(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .ConfigureClient(configuration => configuration.WithGroupId(DefaultConsumerGroupId))))
                    .AddIntegrationSpyAndSubscriber())
            .Run();

        SilverbackStorageInitializer storageInitializer = Host.ScopedServiceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreateSqliteOutboxAsync(new SqliteOutboxSettings(Host.SqliteConnectionString));

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 0; i < 3; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventOne)envelope.Message!).Content)
            .Should().BeEquivalentTo(Enumerable.Range(0, 3).Select(i => $"{i}"));
    }
}
