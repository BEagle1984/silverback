// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using NSubstitute;
using Silverback.Domain;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.EFCore30.TestTypes;
using Silverback.Tests.Core.EFCore30.TestTypes.Model;
using Xunit;

namespace Silverback.Tests.Core.EFCore30;

public sealed class DbContextEventsPublisherTests : IDisposable
{
    private readonly TestDbContext _dbContext;

    private readonly IPublisher _publisher;

    private readonly SqliteConnection _connection;

    public DbContextEventsPublisherTests()
    {
        _publisher = Substitute.For<IPublisher>();

        _connection = new SqliteConnection($"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        _connection.Open();
        DbContextOptions<TestDbContext> dbOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection.ConnectionString)
            .Options;

        _dbContext = new TestDbContext(dbOptions, _publisher);

        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public void SaveChanges_SomeEventsAdded_PublishCalled()
    {
        EntityEntry<TestAggregateRoot> entity = _dbContext.TestAggregates.Add(new TestAggregateRoot());

        entity.Entity.AddEvent<TestDomainEventOne>();
        entity.Entity.AddEvent<TestDomainEventTwo>();
        entity.Entity.AddEvent<TestDomainEventOne>();
        entity.Entity.AddEvent<TestDomainEventTwo>();
        entity.Entity.AddEvent<TestDomainEventOne>();

        _dbContext.SaveChanges();

        _publisher.Received(5).Publish(Arg.Any<IDomainEvent>());
    }

    [Fact]
    public async Task SaveChangesAsync_SomeEventsAdded_PublishCalled()
    {
        EntityEntry<TestAggregateRoot> entity = _dbContext.TestAggregates.Add(new TestAggregateRoot());

        entity.Entity.AddEvent<TestDomainEventOne>();
        entity.Entity.AddEvent<TestDomainEventTwo>();
        entity.Entity.AddEvent<TestDomainEventOne>();
        entity.Entity.AddEvent<TestDomainEventTwo>();
        entity.Entity.AddEvent<TestDomainEventOne>();

        await _dbContext.SaveChangesAsync();

        await _publisher.Received(5).PublishAsync(Arg.Any<IDomainEvent>());
    }

    [Fact]
    public void SaveChanges_SomeEventsAdded_PublishingChainCalled()
    {
        EntityEntry<TestAggregateRoot> entity = _dbContext.TestAggregates.Add(new TestAggregateRoot());

        _publisher
            .When(publisher => publisher.Publish(Arg.Any<TestDomainEventOne>()))
            .Do(_ => entity.Entity.AddEvent<TestDomainEventTwo>());

        entity.Entity.AddEvent<TestDomainEventOne>();

        _dbContext.SaveChanges();

        _publisher.Received(2).Publish(Arg.Any<IDomainEvent>());
    }

    [Fact]
    public async Task SaveChangesAsync_SomeEventsAdded_PublishingChainCalled()
    {
        EntityEntry<TestAggregateRoot> entity = _dbContext.TestAggregates.Add(new TestAggregateRoot());

        _publisher
            .When(publisher => publisher.PublishAsync(Arg.Any<TestDomainEventOne>()))
            .Do(_ => entity.Entity.AddEvent<TestDomainEventTwo>());

        entity.Entity.AddEvent<TestDomainEventOne>();

        await _dbContext.SaveChangesAsync();

        await _publisher.Received(2).PublishAsync(Arg.Any<IDomainEvent>());
    }

    [Fact]
    public async Task SaveChangesAsync_Successful_StartedAndCompleteEventsFired()
    {
        EntityEntry<TestAggregateRoot> entity = _dbContext.TestAggregates.Add(new TestAggregateRoot());

        entity.Entity.AddEvent<TestDomainEventOne>();

        await _dbContext.SaveChangesAsync();

        await _publisher.Received(1).PublishAsync(Arg.Any<TransactionStartedEvent>());
        await _publisher.Received(1).PublishAsync(Arg.Any<TransactionCompletedEvent>());
    }

    [Fact]
    public async Task SaveChangesAsync_Error_StartedAndAbortedEventsFired()
    {
        EntityEntry<TestAggregateRoot> entity = _dbContext.TestAggregates.Add(new TestAggregateRoot());

        _publisher
            .When(publisher => publisher.PublishAsync(Arg.Any<TestDomainEventOne>()))
            .Do(_ => throw new InvalidOperationException());

        entity.Entity.AddEvent<TestDomainEventOne>();

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch
        {
            // ignored
        }

        await _publisher.Received(1).PublishAsync(Arg.Any<TransactionStartedEvent>());
        await _publisher.Received(1).PublishAsync(Arg.Any<TransactionAbortedEvent>());
    }

    public void Dispose()
    {
        _connection.SafeClose();
        _connection.Dispose();
        _dbContext.Dispose();
    }
}
