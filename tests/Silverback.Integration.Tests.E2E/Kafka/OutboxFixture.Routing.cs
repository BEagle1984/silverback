// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Messaging.Publishing;
using Silverback.Storage;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class OutboxFixture
{
    [Fact]
    public async Task Outbox_ShouldProduceMessages_WhenUsingMultipleOutboxes()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka()
                        .AddSqliteOutbox()
                        .AddOutboxWorker(
                            worker => worker
                                .ProcessOutbox(outbox => outbox.UseSqlite(Host.SqliteConnectionString).WithTableName("outbox1"))
                                .WithInterval(TimeSpan.FromMilliseconds(50)))
                        .AddOutboxWorker(
                            worker => worker
                                .ProcessOutbox(outbox => outbox.UseSqlite(Host.SqliteConnectionString).WithTableName("outbox2"))
                                .WithInterval(TimeSpan.FromMilliseconds(50))))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<TestEventOne>(
                                    endpoint => endpoint
                                        .ProduceTo("topic1")
                                        .ProduceToOutbox(
                                            outbox => outbox
                                                .UseSqlite(Host.SqliteConnectionString)
                                                .WithTableName("outbox1")))
                                .Produce<TestEventTwo>(
                                    endpoint => endpoint
                                        .ProduceTo("topic2")
                                        .ProduceToOutbox(
                                            outbox => outbox
                                                .UseSqlite(Host.SqliteConnectionString)
                                                .WithTableName("outbox2"))))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom("topic1", "topic2"))))
                .AddIntegrationSpyAndSubscriber());

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
    public async Task Outbox_ShouldProduceToCorrectTopic_WhenUsingEndpointNameFunction()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka()
                        .AddSqliteOutbox()
                        .AddOutboxWorker(
                            worker => worker
                                .ProcessOutbox(outbox => outbox.UseSqlite(Host.SqliteConnectionString))
                                .WithInterval(TimeSpan.FromMilliseconds(50))))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer
                                .Produce<TestEventOne>(
                                    endpoint => endpoint
                                        .ProduceTo(
                                            message => message?.ContentEventOne switch
                                            {
                                                "1" => "topic1",
                                                "2" => "topic2",
                                                "3" => "topic3",
                                                _ => throw new InvalidOperationException()
                                            })
                                        .ProduceToOutbox(outbox => outbox.UseSqlite(Host.SqliteConnectionString))))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom("topic1", "topic2", "topic3"))))
                .AddIntegrationSpyAndSubscriber());

        SilverbackStorageInitializer storageInitializer = Host.ScopedServiceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreateSqliteOutboxAsync(new SqliteOutboxSettings(Host.SqliteConnectionString));

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "1" });
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "2" });
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "3" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes.Where(envelope => envelope.Endpoint.RawName == "topic1").Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes.Where(envelope => envelope.Endpoint.RawName == "topic2").Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes.Where(envelope => envelope.Endpoint.RawName == "topic3").Should().HaveCount(1);
    }

    [Fact]
    public async Task Outbox_ShouldProduceToCorrectTopic_WhenUsingDynamicNamedEndpoints()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka()
                        .AddSqliteOutbox()
                        .AddOutboxWorker(
                            worker => worker
                                .ProcessOutbox(outbox => outbox.UseSqlite(Host.SqliteConnectionString))
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
                                        .ProduceToOutbox(outbox => outbox.UseSqlite(Host.SqliteConnectionString)))
                                .Produce<TestEventOne>(
                                    "other-topic",
                                    endpoint => endpoint
                                        .ProduceTo(_ => "topic2")
                                        .ProduceToOutbox(outbox => outbox.UseSqlite(Host.SqliteConnectionString))))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom("topic1", "topic2", "topic3"))))
                .AddIntegrationSpyAndSubscriber());

        SilverbackStorageInitializer storageInitializer = Host.ScopedServiceProvider.GetRequiredService<SilverbackStorageInitializer>();
        await storageInitializer.CreateSqliteOutboxAsync(new SqliteOutboxSettings(Host.SqliteConnectionString));

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 0; i < 3; i++)
        {
            await publisher.PublishAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(6);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(6);
        Helper.Spy.InboundEnvelopes.Where(envelope => envelope.Endpoint.RawName == "topic1").Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes.Where(envelope => envelope.Endpoint.RawName == "topic2").Should().HaveCount(3);
    }
}
