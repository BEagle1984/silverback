// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

#if NET8_0

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Silverback.Configuration;
using Silverback.Lock;
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
public class OutboxEntityFrameworkFixture : KafkaFixture
{
    public OutboxEntityFrameworkFixture(ITestOutputHelper testOutputHelper)
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
                .AddDbContextFactory<TestDbContext>(options => options.UseSqlite(database.ConnectionString))
                .InitDbContext<TestDbContext>()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka()
                        .AddEntityFrameworkOutbox()
                        .AddOutboxWorker(
                            worker => worker
                                .ProcessOutbox(outbox => outbox.UseEntityFramework<TestDbContext>())
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
                                        .ProduceToOutbox(outbox => outbox.UseEntityFramework<TestDbContext>())))
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
                .AddDbContextFactory<TestDbContext>(options => options.UseSqlite(database.ConnectionString))
                .InitDbContext<TestDbContext>()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka()
                        .AddEntityFrameworkOutbox()
                        .AddOutboxWorker(
                            worker => worker
                                .ProcessOutbox(outbox => outbox.UseEntityFramework<TestDbContext>())
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
                                        .ProduceToOutbox(outbox => outbox.UseEntityFramework<TestDbContext>())))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishEventsAsync(
            new TestEventOne[]
            {
                new() { ContentEventOne = "1" },
                new() { ContentEventOne = "2" },
                new() { ContentEventOne = "3" }
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
                .AddDbContextFactory<TestDbContext>(options => options.UseSqlite(database.ConnectionString))
                .InitDbContext<TestDbContext>()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka()
                        .AddEntityFrameworkOutbox()
                        .AddOutboxWorker(
                            worker => worker
                                .ProcessOutbox(outbox => outbox.UseEntityFramework<TestDbContext>())
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
                                        .ProduceToOutbox(outbox => outbox.UseEntityFramework<TestDbContext>())))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishEventsAsync(
            new TestEventOne[]
            {
                new() { ContentEventOne = "1" },
                new() { ContentEventOne = "2" },
                new() { ContentEventOne = "3" }
            }.ToAsyncEnumerable());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne)
            .Should().BeEquivalentTo(Enumerable.Range(1, 3).Select(i => $"{i}"));
    }

    [Fact]
    public async Task Outbox_ShouldUseTransaction_WhenUsingSqlite()
    {
        using SqliteDatabase database = await SqliteDatabase.StartAsync();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddDbContextFactory<TestDbContext>(options => options.UseSqlite(database.ConnectionString))
                .InitDbContext<TestDbContext>()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka()
                        .AddEntityFrameworkOutbox()
                        .AddOutboxWorker(
                            worker => worker
                                .ProcessOutbox(outbox => outbox.UseEntityFramework<TestDbContext>())
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
                                        .ProduceToOutbox(outbox => outbox.UseEntityFramework<TestDbContext>())))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        using TestDbContext dbContext = Host.ServiceProvider.GetRequiredService<IDbContextFactory<TestDbContext>>().CreateDbContext();

        await using (IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync())
        {
            IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
            await using IStorageTransaction storageTransaction = publisher.EnlistDbTransaction(transaction.GetDbTransaction());

            for (int i = 0; i < 3; i++)
            {
                await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"rollback {i}" });
            }

            await transaction.RollbackAsync();
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(0);

        await using (IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync())
        {
            IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
            await using IStorageTransaction storageTransaction = publisher.EnlistDbTransaction(transaction.GetDbTransaction());

            for (int i = 0; i < 3; i++)
            {
                await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"commit {i}" });
            }

            await transaction.CommitAsync();
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(6);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne)
            .Should().BeEquivalentTo(Enumerable.Range(0, 3).Select(i => $"commit {i}"));
    }

    [Fact]
    [Trait("Dependency", "Docker")]
    [Trait("Database", "PostgreSql")]
    public async Task Outbox_ShouldUseTransaction_WhenUsingPostgreSql()
    {
        using PostgreSqlDatabase database = await PostgreSqlDatabase.StartAsync();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddDbContextFactory<TestDbContext>(options => options.UseNpgsql(database.ConnectionString))
                .InitDbContext<TestDbContext>()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka()
                        .AddEntityFrameworkOutbox()
                        .AddOutboxWorker(
                            worker => worker
                                .ProcessOutbox(outbox => outbox.UseEntityFramework<TestDbContext>())
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
                                        .ProduceToOutbox(outbox => outbox.UseEntityFramework<TestDbContext>())))
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        using TestDbContext dbContext = Host.ServiceProvider.GetRequiredService<IDbContextFactory<TestDbContext>>().CreateDbContext();

        await using (IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync())
        {
            IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
            await using IStorageTransaction storageTransaction = publisher.EnlistDbTransaction(transaction.GetDbTransaction());

            for (int i = 0; i < 3; i++)
            {
                await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"rollback {i}" });
            }

            await transaction.RollbackAsync();
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(0);

        await using (IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync())
        {
            IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
            await using IStorageTransaction storageTransaction = publisher.EnlistDbTransaction(transaction.GetDbTransaction());

            for (int i = 0; i < 3; i++)
            {
                await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"commit {i}" });
            }

            await transaction.CommitAsync();
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(6);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne)
            .Should().BeEquivalentTo(Enumerable.Range(0, 3).Select(i => $"commit {i}"));
    }

    private class TestDbContext : DbContext
    {
        [ActivatorUtilitiesConstructor] // TODO: Can be removed? https://github.com/dotnet/efcore/issues/25273
        public TestDbContext(DbContextOptions options)
            : base(options)
        {
        }

        public TestDbContext(SqliteConnection connection)
            : base(new DbContextOptionsBuilder().UseSqlite(connection).Options)
        {
        }

        public TestDbContext(NpgsqlConnection connection)
            : base(new DbContextOptionsBuilder().UseNpgsql(connection).Options)
        {
        }

        public DbSet<SilverbackOutboxMessage> Outbox { get; set; } = null!;

        public DbSet<SilverbackLock> Locks { get; set; } = null!;
    }
}

#endif
