// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NSubstitute;
using Shouldly;
using Silverback.Domain;
using Silverback.Storage;
using Xunit;

namespace Silverback.Tests.Storage.EntityFramework.Domain;

public sealed partial class EntityFrameworkDomainEventsPublisherFixture
{
    [Fact]
    public async Task SaveChangesAndPublishDomainEventsAsync_ShouldPublishEventsAndStoreEntities()
    {
        TestDomainEntity entity1 = _dbContext.TestDomainEntities.Add(new TestDomainEntity()).Entity;
        TestDomainEntity entity2 = _dbContext.TestDomainEntities.Add(new TestDomainEntity()).Entity;
        await _dbContext.SaveChangesAsync();

        entity1.SetValue(42);
        entity2.SetValue(42);

        EntityFrameworkDomainEventsPublisher<TestDbContext> domainEventsPublisher = new(_dbContext, _publisher);

        await domainEventsPublisher.SaveChangesAndPublishDomainEventsAsync();

        await _publisher.Received(2).PublishAsync(Arg.Any<ValueChangedDomainEvent>());
        _dbContext.ChangeTracker.Entries().ShouldAllBe(entry => entry.State == EntityState.Unchanged);
        _assertDbContext.TestDomainEntities.Count().ShouldBe(2);
        _assertDbContext.TestDomainEntities.ShouldAllBe(entity => entity.Value == 42);
    }

    [Fact]
    public async Task SaveChangesAndPublishDomainEventsAsync_ShouldCreateAndShareTransaction()
    {
        TestDomainEntity entity1 = _dbContext.TestDomainEntities.Add(new TestDomainEntity()).Entity;
        TestDomainEntity entity2 = _dbContext.TestDomainEntities.Add(new TestDomainEntity()).Entity;
        await _dbContext.SaveChangesAsync();

        entity1.SetValue(42);
        entity2.SetValue(42);

        EntityFrameworkDomainEventsPublisher<TestDbContext> domainEventsPublisher = new(_dbContext, _publisher);

        await domainEventsPublisher.SaveChangesAndPublishDomainEventsAsync();

        await _publisher.Received(2).PublishAsync(Arg.Any<ValueChangedDomainEvent>());
        _publisher.Context.Received(1).AddObject(Arg.Any<Guid>(), Arg.Is<IStorageTransaction>(transaction => transaction != null));
        _dbContext.ChangeTracker.Entries().ShouldAllBe(entry => entry.State == EntityState.Unchanged);
        _assertDbContext.TestDomainEntities.Count().ShouldBe(2);
        _assertDbContext.TestDomainEntities.ShouldAllBe(entity => entity.Value == 42);
    }

    [Fact]
    public async Task SaveChangesAndPublishDomainEventsAsync_ShouldUseExistingTransaction()
    {
        TestDomainEntity entity1 = _dbContext.TestDomainEntities.Add(new TestDomainEntity()).Entity;
        TestDomainEntity entity2 = _dbContext.TestDomainEntities.Add(new TestDomainEntity()).Entity;
        await _dbContext.SaveChangesAsync();

        entity1.SetValue(42);
        entity2.SetValue(42);

        EntityFrameworkDomainEventsPublisher<TestDbContext> domainEventsPublisher = new(_dbContext, _publisher);

        await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync();

        await domainEventsPublisher.SaveChangesAndPublishDomainEventsAsync();

        await transaction.RollbackAsync();

        await _publisher.Received(2).PublishAsync(Arg.Any<ValueChangedDomainEvent>());
        _dbContext.ChangeTracker.Entries().ShouldAllBe(entry => entry.State == EntityState.Unchanged);
        _assertDbContext.TestDomainEntities.Count().ShouldBe(2);
        _assertDbContext.TestDomainEntities.ShouldAllBe(entity => entity.Value == 0);
    }
}
