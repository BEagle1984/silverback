// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
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
    public void SaveChangesAndPublishDomainEvents_ShouldPublishEventsAndStoreEntities()
    {
        TestDomainEntity entity1 = _dbContext.TestDomainEntities.Add(new TestDomainEntity()).Entity;
        TestDomainEntity entity2 = _dbContext.TestDomainEntities.Add(new TestDomainEntity()).Entity;
        _dbContext.SaveChanges();

        entity1.SetValue(42);
        entity2.SetValue(42);

        EntityFrameworkDomainEventsPublisher<TestDbContext> domainEventsPublisher = new(_dbContext, _publisher);

        domainEventsPublisher.SaveChangesAndPublishDomainEvents();

        _publisher.Received(2).Publish(Arg.Any<ValueChangedDomainEvent>());
        _dbContext.ChangeTracker.Entries().ShouldAllBe(entry => entry.State == EntityState.Unchanged);
        _assertDbContext.TestDomainEntities.Count().ShouldBe(2);
        _assertDbContext.TestDomainEntities.ShouldAllBe(entry => entry.Value == 42);
    }

    [Fact]
    public void SaveChangesAndPublishDomainEvents_ShouldCreateAndShareTransaction()
    {
        TestDomainEntity entity1 = _dbContext.TestDomainEntities.Add(new TestDomainEntity()).Entity;
        TestDomainEntity entity2 = _dbContext.TestDomainEntities.Add(new TestDomainEntity()).Entity;
        _dbContext.SaveChanges();

        entity1.SetValue(42);
        entity2.SetValue(42);

        EntityFrameworkDomainEventsPublisher<TestDbContext> domainEventsPublisher = new(_dbContext, _publisher);

        domainEventsPublisher.SaveChangesAndPublishDomainEvents();

        _publisher.Received(2).Publish(Arg.Any<ValueChangedDomainEvent>());
        _publisher.Context.Received(1).AddObject(Arg.Any<Guid>(), Arg.Is<IStorageTransaction>(transaction => transaction != null));
        _dbContext.ChangeTracker.Entries().ShouldAllBe(entry => entry.State == EntityState.Unchanged);
        _assertDbContext.TestDomainEntities.Count().ShouldBe(2);
        _assertDbContext.TestDomainEntities.ShouldAllBe(entry => entry.Value == 42);
    }

    [Fact]
    public void SaveChangesAndPublishDomainEvents_ShouldUseExistingTransaction()
    {
        TestDomainEntity entity1 = _dbContext.TestDomainEntities.Add(new TestDomainEntity()).Entity;
        TestDomainEntity entity2 = _dbContext.TestDomainEntities.Add(new TestDomainEntity()).Entity;
        _dbContext.SaveChanges();

        entity1.SetValue(42);
        entity2.SetValue(42);

        EntityFrameworkDomainEventsPublisher<TestDbContext> domainEventsPublisher = new(_dbContext, _publisher);

        using IDbContextTransaction transaction = _dbContext.Database.BeginTransaction();

        domainEventsPublisher.SaveChangesAndPublishDomainEvents();

        transaction.Rollback();

        _publisher.Received(2).Publish(Arg.Any<ValueChangedDomainEvent>());
        _dbContext.ChangeTracker.Entries().ShouldAllBe(entry => entry.State == EntityState.Unchanged);
        _assertDbContext.TestDomainEntities.Count().ShouldBe(2);
        _assertDbContext.TestDomainEntities.ShouldAllBe(entity => entity.Value == 0);
    }
}
