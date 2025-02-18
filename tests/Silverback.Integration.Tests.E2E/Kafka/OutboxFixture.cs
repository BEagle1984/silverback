// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
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
                                        .StoreToOutbox(outbox => outbox.UseSqlite(database.ConnectionString)))
                                .Produce<TestEventTwo>(
                                    "my-endpoint-2",
                                    endpoint => endpoint
                                        .ProduceTo("topic2")
                                        .StoreToOutbox(outbox => outbox.UseSqlite(database.ConnectionString))))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom("topic1", "topic2", "topic3"))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(new TestEventOne());
        await publisher.PublishEventAsync(new TestEventTwo());
        await publisher.PublishEventAsync(new TestEventOne());
        await publisher.PublishEventAsync(new TestEventTwo());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(4);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(4);
        Helper.Spy.InboundEnvelopes.Count(envelope => envelope.Endpoint.RawName == "topic1").ShouldBe(2);
        Helper.Spy.InboundEnvelopes.Count(envelope => envelope.Endpoint.RawName == "topic2").ShouldBe(2);
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
                                        .StoreToOutbox(outbox => outbox.UseSqlite(database.ConnectionString))))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom("topic1", "topic2", "topic3"))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "2" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "3" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(3);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(3);
        Helper.Spy.InboundEnvelopes.Count(envelope => envelope.Endpoint.RawName == "topic1").ShouldBe(1);
        Helper.Spy.InboundEnvelopes.Count(envelope => envelope.Endpoint.RawName == "topic2").ShouldBe(1);
        Helper.Spy.InboundEnvelopes.Count(envelope => envelope.Endpoint.RawName == "topic3").ShouldBe(1);
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
                                        .ProduceTo((TestEventOne? _) => "topic1")
                                        .StoreToOutbox(outbox => outbox.UseSqlite(database.ConnectionString)))
                                .Produce<TestEventOne>(
                                    "other-topic",
                                    endpoint => endpoint
                                        .ProduceTo((IOutboundEnvelope<TestEventOne> _) => "topic2")
                                        .StoreToOutbox(outbox => outbox.UseSqlite(database.ConnectionString))))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom("topic1", "topic2", "topic3"))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 0; i < 3; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(6);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(6);
        Helper.Spy.InboundEnvelopes.Count(envelope => envelope.Endpoint.RawName == "topic1").ShouldBe(3);
        Helper.Spy.InboundEnvelopes.Count(envelope => envelope.Endpoint.RawName == "topic2").ShouldBe(3);
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
                                .ProcessOutbox(outbox => outbox.UseSqlite(database.ConnectionString).UseTable("outbox1"))
                                .WithInterval(TimeSpan.FromMilliseconds(50)))
                        .AddOutboxWorker(
                            worker => worker
                                .ProcessOutbox(outbox => outbox.UseSqlite(database.ConnectionString).UseTable("outbox2"))
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
                                        .StoreToOutbox(
                                            outbox => outbox
                                                .UseSqlite(database.ConnectionString)
                                                .UseTable("outbox1")))
                                .Produce<TestEventTwo>(
                                    "my-endpoint-2",
                                    endpoint => endpoint
                                        .ProduceTo("topic2")
                                        .StoreToOutbox(
                                            outbox => outbox
                                                .UseSqlite(database.ConnectionString)
                                                .UseTable("outbox2"))))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom("topic1", "topic2"))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 0; i < 3; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne());
            await publisher.PublishEventAsync(new TestEventTwo());
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(6);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(6);
        List<object?> inboundMessages = Helper.Spy.InboundEnvelopes.Select(envelope => envelope.Message).ToList();

        inboundMessages.OfType<TestEventOne>().Count().ShouldBe(3);
        inboundMessages.OfType<TestEventTwo>().Count().ShouldBe(3);
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
                                        .StoreToOutbox(outbox => outbox.UseMemory()))))
                .AddIntegrationSpy());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.WrapAndPublishAsync(new TestEventOne(), envelope => envelope.SetKafkaKey("key"));

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        DefaultTopic.MessagesCount.ShouldBe(1);
        DefaultTopic.GetAllMessages()[0].Key.ShouldBe("key"u8.ToArray());
    }
}
