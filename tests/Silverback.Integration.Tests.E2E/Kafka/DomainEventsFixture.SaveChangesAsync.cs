// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestHost.Database;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

[SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Test code")]
public partial class DomainEventsFixture
{
    [Fact]
    public async Task DomainEvents_ShouldBeProducedDuringSaveChangesAsync()
    {
        using SqliteDatabase database = await SqliteDatabase.StartAsync();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddDbContext<TestDbContext>(options => options.UseSqlite(database.ConnectionString))
                .InitDbContext<TestDbContext>()
                .AddSilverback()
                .AddDelegateSubscriber<ValueChangedDomainEvent, TestEventOne>(HandleDomainEvent)
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer.Produce<IIntegrationEvent>(
                                endpoint => endpoint
                                    .ProduceTo(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        static TestEventOne HandleDomainEvent(ValueChangedDomainEvent domainEvent) => new() { ContentEventOne = $"new value: {domainEvent.Source?.Value}" };

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            TestDomainEntity entity = dbContext.TestDomainEntities.Add(new TestDomainEntity()).Entity;
            entity.SetValue(42);
            await dbContext.SaveChangesAsync();
        }

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.OutboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
        Helper.Spy.OutboundEnvelopes[0].Message.As<TestEventOne>().ContentEventOne.Should().Be("new value: 42");

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            TestDomainEntity entity = dbContext.TestDomainEntities.First();
            entity.SetValue(42000);
            await dbContext.SaveChangesAsync();
        }

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.OutboundEnvelopes[1].Message.Should().BeOfType<TestEventOne>();
        Helper.Spy.OutboundEnvelopes[1].Message.As<TestEventOne>().ContentEventOne.Should().Be("new value: 42000");
    }

    [Fact]
    public async Task DomainEvents_ShouldBeStoredToOutboxDuringSaveChangesAsync()
    {
        using SqliteDatabase database = await SqliteDatabase.StartAsync();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddDbContext<TestDbContext>(options => options.UseSqlite(database.ConnectionString))
                .InitDbContext<TestDbContext>()
                .AddSilverback()
                .AddDelegateSubscriber<ValueChangedDomainEvent, TestEventOne>(HandleDomainEvent)
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka()
                        .AddEntityFrameworkOutbox())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer.Produce<IIntegrationEvent>(
                                "test",
                                endpoint => endpoint
                                    .ProduceTo(DefaultTopicName)
                                    .StoreToOutbox(outbox => outbox.UseEntityFramework<TestDbContext>())))));

        static TestEventOne HandleDomainEvent(ValueChangedDomainEvent domainEvent) => new() { ContentEventOne = $"new value: {domainEvent.Source?.Value}" };

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            TestDomainEntity entity = dbContext.TestDomainEntities.Add(new TestDomainEntity()).Entity;
            entity.SetValue(42);
            await dbContext.SaveChangesAsync();
        }

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            dbContext.TestDomainEntities.Should().HaveCount(1);
            dbContext.Outbox.Should().HaveCount(1);
        }

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            TestDomainEntity entity = dbContext.TestDomainEntities.First();
            entity.SetValue(42000);
            await dbContext.SaveChangesAsync();
        }

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            dbContext.TestDomainEntities.Should().HaveCount(1);
            dbContext.Outbox.Should().HaveCount(2);
        }
    }

    [Fact]
    public async Task DomainEvents_ShouldBeProducedDuringSaveChangesAsync_WhenTransactionExists()
    {
        using SqliteDatabase database = await SqliteDatabase.StartAsync();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddDbContext<TestDbContext>(options => options.UseSqlite(database.ConnectionString))
                .InitDbContext<TestDbContext>()
                .AddSilverback()
                .AddDelegateSubscriber<ValueChangedDomainEvent, TestEventOne>(HandleDomainEvent)
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer.Produce<IIntegrationEvent>(
                                endpoint => endpoint
                                    .ProduceTo(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        static TestEventOne HandleDomainEvent(ValueChangedDomainEvent domainEvent) => new() { ContentEventOne = $"new value: {domainEvent.Source?.Value}" };

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            TestDomainEntity entity = dbContext.TestDomainEntities.Add(new TestDomainEntity()).Entity;
            entity.SetValue(42);
            await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync();

            await dbContext.SaveChangesAsync();

            await transaction.RollbackAsync();
        }

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            dbContext.TestDomainEntities.Should().HaveCount(0);
        }

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            TestDomainEntity entity = dbContext.TestDomainEntities.Add(new TestDomainEntity()).Entity;
            entity.SetValue(42);
            await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync();

            await dbContext.SaveChangesAsync();

            await transaction.CommitAsync();
        }

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            dbContext.TestDomainEntities.Should().HaveCount(1);
        }
    }

    [Fact]
    public async Task DomainEvents_ShouldBeStoredToOutboxDuringSaveChangesAsync_WhenTransactionExists()
    {
        using SqliteDatabase database = await SqliteDatabase.StartAsync();

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddDbContext<TestDbContext>(options => options.UseSqlite(database.ConnectionString))
                .InitDbContext<TestDbContext>()
                .AddSilverback()
                .AddDelegateSubscriber<ValueChangedDomainEvent, TestEventOne>(HandleDomainEvent)
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka()
                        .AddEntityFrameworkOutbox())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer.Produce<IIntegrationEvent>(
                                "test",
                                endpoint => endpoint
                                    .ProduceTo(DefaultTopicName)
                                    .StoreToOutbox(outbox => outbox.UseEntityFramework<TestDbContext>())))));

        static TestEventOne HandleDomainEvent(ValueChangedDomainEvent domainEvent) => new() { ContentEventOne = $"new value: {domainEvent.Source?.Value}" };

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            TestDomainEntity entity = dbContext.TestDomainEntities.Add(new TestDomainEntity()).Entity;
            entity.SetValue(42);
            await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync();

            await dbContext.SaveChangesAsync();

            await transaction.RollbackAsync();

            dbContext.TestDomainEntities.Should().HaveCount(0);
            dbContext.Outbox.Should().HaveCount(0);
        }

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            dbContext.TestDomainEntities.Should().HaveCount(0);
            dbContext.Outbox.Should().HaveCount(0);
        }

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            TestDomainEntity entity = dbContext.TestDomainEntities.Add(new TestDomainEntity()).Entity;
            entity.SetValue(42);
            await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync();

            await dbContext.SaveChangesAsync();

            await transaction.CommitAsync();
        }

        using (IServiceScope scope = Host.ServiceProvider.CreateScope())
        {
            TestDbContext dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
            dbContext.TestDomainEntities.Should().HaveCount(1);
            dbContext.Outbox.Should().HaveCount(1);
        }
    }
}
