// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Storage;
using Silverback.Storage.Relational;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestHost.Database;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

[SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Test code")]
[Trait("Dependency", "Docker")]
[Trait("Database", "PostgreSql")]
public class OutboxPostgreSqlFixture : KafkaFixture
{
    public OutboxPostgreSqlFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Outbox_ShouldProduceMessages_WhenUsingPostgreSql()
    {
        using PostgreSqlDatabase database = await PostgreSqlDatabase.StartAsync();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .InitDatabase(storageInitializer => storageInitializer.CreatePostgreSqlOutboxAsync(database.ConnectionString))
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka()
                        .AddPostgreSqlOutbox()
                        .AddOutboxWorker(
                            worker => worker
                                .ProcessOutbox(outbox => outbox.UsePostgreSql(database.ConnectionString))
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
                                        .ProduceToOutbox(outbox => outbox.UsePostgreSql(database.ConnectionString))))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 0; i < 3; i++)
        {
            await publisher.PublishAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne)
            .Should().BeEquivalentTo(Enumerable.Range(0, 3).Select(i => $"{i}"));
    }

    [Fact]
    public async Task Outbox_ShouldProduceBatch_WhenUsingPostgreSql()
    {
        using PostgreSqlDatabase database = await PostgreSqlDatabase.StartAsync();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .InitDatabase(storageInitializer => storageInitializer.CreatePostgreSqlOutboxAsync(database.ConnectionString))
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka()
                        .AddPostgreSqlOutbox()
                        .AddOutboxWorker(
                            worker => worker
                                .ProcessOutbox(outbox => outbox.UsePostgreSql(database.ConnectionString))
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
                                        .ProduceToOutbox(outbox => outbox.UsePostgreSql(database.ConnectionString))))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(
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
    public async Task Outbox_ShouldProduceAsyncBatch_WhenUsingPostgreSql()
    {
        using PostgreSqlDatabase database = await PostgreSqlDatabase.StartAsync();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .InitDatabase(storageInitializer => storageInitializer.CreatePostgreSqlOutboxAsync(database.ConnectionString))
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka()
                        .AddPostgreSqlOutbox()
                        .AddOutboxWorker(
                            worker => worker
                                .ProcessOutbox(outbox => outbox.UsePostgreSql(database.ConnectionString))
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
                                        .ProduceToOutbox(outbox => outbox.UsePostgreSql(database.ConnectionString))))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishAsync(
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
    public async Task Outbox_ShouldUseTransaction_WhenUsingPostgreSql()
    {
        using PostgreSqlDatabase database = await PostgreSqlDatabase.StartAsync();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .InitDatabase(storageInitializer => storageInitializer.CreatePostgreSqlOutboxAsync(database.ConnectionString))
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka()
                        .AddPostgreSqlOutbox()
                        .AddOutboxWorker(
                            worker => worker
                                .ProcessOutbox(outbox => outbox.UsePostgreSql(database.ConnectionString))
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
                                        .ProduceToOutbox(outbox => outbox.UsePostgreSql(database.ConnectionString))))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        await using NpgsqlConnection connection = new(database.ConnectionString);
        await connection.OpenAsync();

        await using (DbTransaction transaction = await connection.BeginTransactionAsync())
        {
            IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await using IStorageTransaction storageTransaction = publisher.EnlistDbTransaction(transaction);

            for (int i = 0; i < 3; i++)
            {
                await publisher.PublishAsync(new TestEventOne { ContentEventOne = $"rollback {i}" });
            }

            await transaction.RollbackAsync();
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(0);

        await using (DbTransaction transaction = await connection.BeginTransactionAsync())
        {
            IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await using IStorageTransaction storageTransaction = publisher.EnlistDbTransaction(transaction);

            for (int i = 0; i < 3; i++)
            {
                await publisher.PublishAsync(new TestEventOne { ContentEventOne = $"commit {i}" });
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
