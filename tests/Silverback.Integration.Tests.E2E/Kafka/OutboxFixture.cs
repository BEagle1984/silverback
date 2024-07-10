// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Messaging.Publishing;
using Silverback.Storage;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestHost.Database;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

[SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Test code")]
public class OutboxFixture : KafkaFixture
{
    public OutboxFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Outbox_ShouldProduceToCorrectTopic()
    {
        using SqliteDatabase database = await SqliteDatabase.StartAsync();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .InitDatabase(storageInitializer => storageInitializer.CreateSqliteOutboxAsync(new SqliteOutboxSettings(database.ConnectionString)))
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka()
                        .AddSqliteOutbox()
                        .AddOutboxWorker(
                            worker => worker
                                .ProcessOutbox(outbox => outbox.UseSqlite(database.ConnectionString))
                                .WithInterval(TimeSpan.FromMilliseconds(50))))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<TestEventOne>(
                                    "my-endpoint-1",
                                    endpoint => endpoint
                                        .ProduceTo("topic1")
                                        .ProduceToOutbox(outbox => outbox.UseSqlite(database.ConnectionString)))
                                .Produce<TestEventTwo>(
                                    "my-endpoint-2",
                                    endpoint => endpoint
                                        .ProduceTo("topic2")
                                        .ProduceToOutbox(outbox => outbox.UseSqlite(database.ConnectionString))))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom("topic1", "topic2", "topic3"))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(new TestEventOne());
        await publisher.PublishEventAsync(new TestEventTwo());
        await publisher.PublishEventAsync(new TestEventOne());
        await publisher.PublishEventAsync(new TestEventTwo());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(4);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(4);
        Helper.Spy.InboundEnvelopes.Where(envelope => envelope.Endpoint.RawName == "topic1").Should().HaveCount(2);
        Helper.Spy.InboundEnvelopes.Where(envelope => envelope.Endpoint.RawName == "topic2").Should().HaveCount(2);
    }

    [Fact]
    public async Task Outbox_ShouldProduceToCorrectTopic_WhenUsingEndpointNameFunction()
    {
        using SqliteDatabase database = await SqliteDatabase.StartAsync();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .InitDatabase(storageInitializer => storageInitializer.CreateSqliteOutboxAsync(new SqliteOutboxSettings(database.ConnectionString)))
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka()
                        .AddSqliteOutbox()
                        .AddOutboxWorker(
                            worker => worker
                                .ProcessOutbox(outbox => outbox.UseSqlite(database.ConnectionString))
                                .WithInterval(TimeSpan.FromMilliseconds(50))))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<TestEventOne>(
                                    "my-dynamic-endpoint",
                                    endpoint => endpoint
                                        .ProduceTo(
                                            message => message?.ContentEventOne switch
                                            {
                                                "1" => "topic1",
                                                "2" => "topic2",
                                                "3" => "topic3",
                                                _ => throw new InvalidOperationException()
                                            })
                                        .ProduceToOutbox(outbox => outbox.UseSqlite(database.ConnectionString))))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom("topic1", "topic2", "topic3"))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "2" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes.Where(envelope => envelope.Endpoint.RawName == "topic1").Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes.Where(envelope => envelope.Endpoint.RawName == "topic2").Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes.Where(envelope => envelope.Endpoint.RawName == "topic3").Should().HaveCount(1);
    }

    [Fact]
    public async Task Outbox_ShouldProduceToCorrectTopic_WhenUsingMultipleNamedEndpoints()
    {
        using SqliteDatabase database = await SqliteDatabase.StartAsync();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .InitDatabase(storageInitializer => storageInitializer.CreateSqliteOutboxAsync(new SqliteOutboxSettings(database.ConnectionString)))
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka()
                        .AddSqliteOutbox()
                        .AddOutboxWorker(
                            worker => worker
                                .ProcessOutbox(outbox => outbox.UseSqlite(database.ConnectionString))
                                .WithInterval(TimeSpan.FromMilliseconds(50))))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<TestEventOne>(
                                    "my-topic",
                                    endpoint => endpoint
                                        .ProduceTo(_ => "topic1")
                                        .ProduceToOutbox(outbox => outbox.UseSqlite(database.ConnectionString)))
                                .Produce<TestEventOne>(
                                    "other-topic",
                                    endpoint => endpoint
                                        .ProduceTo(_ => "topic2")
                                        .ProduceToOutbox(outbox => outbox.UseSqlite(database.ConnectionString))))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom("topic1", "topic2", "topic3"))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 0; i < 3; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(6);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(6);
        Helper.Spy.InboundEnvelopes.Where(envelope => envelope.Endpoint.RawName == "topic1").Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes.Where(envelope => envelope.Endpoint.RawName == "topic2").Should().HaveCount(3);
    }

    [Fact]
    public async Task Outbox_ShouldProduce_WhenUsingMultipleOutboxes()
    {
        using SqliteDatabase database = await SqliteDatabase.StartAsync();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .InitDatabase(
                    async storageInitializer =>
                    {
                        await storageInitializer.CreateSqliteOutboxAsync(
                            new SqliteOutboxSettings(database.ConnectionString)
                            {
                                TableName = "outbox1"
                            });
                        await storageInitializer.CreateSqliteOutboxAsync(
                            new SqliteOutboxSettings(database.ConnectionString)
                            {
                                TableName = "outbox2"
                            });
                    })
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka()
                        .AddSqliteOutbox()
                        .AddOutboxWorker(
                            worker => worker
                                .ProcessOutbox(outbox => outbox.UseSqlite(database.ConnectionString).WithTableName("outbox1"))
                                .WithInterval(TimeSpan.FromMilliseconds(50)))
                        .AddOutboxWorker(
                            worker => worker
                                .ProcessOutbox(outbox => outbox.UseSqlite(database.ConnectionString).WithTableName("outbox2"))
                                .WithInterval(TimeSpan.FromMilliseconds(50))))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<TestEventOne>(
                                    "my-endpoint-1",
                                    endpoint => endpoint
                                        .ProduceTo("topic1")
                                        .ProduceToOutbox(
                                            outbox => outbox
                                                .UseSqlite(database.ConnectionString)
                                                .WithTableName("outbox1")))
                                .Produce<TestEventTwo>(
                                    "my-endpoint-2",
                                    endpoint => endpoint
                                        .ProduceTo("topic2")
                                        .ProduceToOutbox(
                                            outbox => outbox
                                                .UseSqlite(database.ConnectionString)
                                                .WithTableName("outbox2"))))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom("topic1", "topic2"))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 0; i < 3; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne());
            await publisher.PublishEventAsync(new TestEventTwo());
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(6);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(6);
        List<object?> inboundMessages = Helper.Spy.InboundEnvelopes.Select(envelope => envelope.Message).ToList();

        inboundMessages.OfType<TestEventOne>().Should().HaveCount(3);
        inboundMessages.OfType<TestEventTwo>().Should().HaveCount(3);
    }

    [Fact]
    public async Task Outbox_ShouldPreserveKafkaKey()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka()
                        .AddInMemoryOutbox()
                        .AddOutboxWorker(
                            worker => worker
                                .ProcessOutbox(outbox => outbox.UseMemory())
                                .WithInterval(TimeSpan.FromMilliseconds(50))))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<IIntegrationEvent>(
                                    "my-endpoint",
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .ProduceToOutbox(outbox => outbox.UseMemory()))))
                .AddIntegrationSpy());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.WrapAndPublishAsync(new TestEventOne(), envelope => envelope.SetKafkaKey("key"));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        DefaultTopic.MessagesCount.Should().Be(1);
        DefaultTopic.GetAllMessages()[0].Key.Should().BeEquivalentTo("key"u8.ToArray());
    }
}
