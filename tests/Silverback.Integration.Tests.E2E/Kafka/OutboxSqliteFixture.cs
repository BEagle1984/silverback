﻿// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Storage;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestHost.Database;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

[SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Test code")]
public class OutboxSqliteFixture : KafkaFixture
{
    public OutboxSqliteFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Outbox_ShouldProduceMessages()
    {
        using SqliteDatabase database = await SqliteDatabase.StartAsync();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .InitDatabase(storageInitializer => storageInitializer.CreateSqliteOutboxAsync(database.ConnectionString))
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
                                .Produce<IIntegrationEvent>(
                                    "my-endpoint",
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .ProduceToOutbox(outbox => outbox.UseSqlite(database.ConnectionString))))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 0; i < 3; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne)
            .Should().BeEquivalentTo(Enumerable.Range(0, 3).Select(i => $"{i}"));
    }

    [Fact]
    public async Task Outbox_ShouldProduceBatch()
    {
        using SqliteDatabase database = await SqliteDatabase.StartAsync();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .InitDatabase(storageInitializer => storageInitializer.CreateSqliteOutboxAsync(database.ConnectionString))
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
                                .Produce<IIntegrationEvent>(
                                    "my-endpoint",
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .ProduceToOutbox(outbox => outbox.UseSqlite(database.ConnectionString))))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.WrapAndPublishBatchAsync(
            new TestEventOne[]
            {
                new() { ContentEventOne = "1" },
                new() { ContentEventOne = "2" },
                new() { ContentEventOne = "3" },
            });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne)
            .Should().BeEquivalentTo(Enumerable.Range(1, 3).Select(i => $"{i}"));
    }

    [Fact]
    public async Task Outbox_ShouldProduceAsyncBatch()
    {
        using SqliteDatabase database = await SqliteDatabase.StartAsync();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .InitDatabase(storageInitializer => storageInitializer.CreateSqliteOutboxAsync(database.ConnectionString))
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
                                .Produce<IIntegrationEvent>(
                                    "my-endpoint",
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .ProduceToOutbox(outbox => outbox.UseSqlite(database.ConnectionString))))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.WrapAndPublishBatchAsync(
            new TestEventOne[]
            {
                new() { ContentEventOne = "1" },
                new() { ContentEventOne = "2" },
                new() { ContentEventOne = "3" },
            }.ToAsyncEnumerable());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne)
            .Should().BeEquivalentTo(Enumerable.Range(1, 3).Select(i => $"{i}"));
    }

    [Fact]
    public async Task Outbox_ShouldUseTransaction()
    {
        using SqliteDatabase database = await SqliteDatabase.StartAsync();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .InitDatabase(storageInitializer => storageInitializer.CreateSqliteOutboxAsync(database.ConnectionString))
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
                                .Produce<IIntegrationEvent>(
                                    "my-endpoint",
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .ProduceToOutbox(outbox => outbox.UseSqlite(database.ConnectionString))))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        await using SqliteConnection connection = new(database.ConnectionString);
        await connection.OpenAsync();

        await using (DbTransaction transaction = await connection.BeginTransactionAsync())
        {
            IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
            await using IStorageTransaction storageTransaction = publisher.EnlistDbTransaction(transaction);

            for (int i = 0; i < 3; i++)
            {
                await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"rollback {i}" });
            }

            await transaction.RollbackAsync();
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(0);

        await using (DbTransaction transaction = await connection.BeginTransactionAsync())
        {
            IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
            await using IStorageTransaction storageTransaction = publisher.EnlistDbTransaction(transaction);

            for (int i = 0; i < 3; i++)
            {
                await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"commit {i}" });
            }

            await transaction.CommitAsync();
        }

        await connection.CloseAsync();

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(6);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne)
            .Should().BeEquivalentTo(Enumerable.Range(0, 3).Select(i => $"commit {i}"));
    }
}
