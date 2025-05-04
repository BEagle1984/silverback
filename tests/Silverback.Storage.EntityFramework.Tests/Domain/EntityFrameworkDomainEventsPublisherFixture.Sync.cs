// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

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
    public void SaveChangesAndPublishDomainEvents_ShouldClearAndRestoreStorageTransaction_WhenContextHasNoTransaction()
    {
        IStorageTransaction storageTransaction = Substitute.For<IStorageTransaction>();
        _publisher.Context.TryGetObject(SilverbackContextStorageExtensions.StorageTransactionObjectTypeId, out Arg.Any<IStorageTransaction?>())
            .Returns(callInfo =>
            {
                callInfo[1] = storageTransaction;
                return true;
            });

        TestDomainEntity entity1 = _dbContext.TestDomainEntities.Add(new TestDomainEntity()).Entity;
        TestDomainEntity entity2 = _dbContext.TestDomainEntities.Add(new TestDomainEntity()).Entity;
        _dbContext.SaveChanges();

        entity1.SetValue(42);
        entity2.SetValue(42);

        EntityFrameworkDomainEventsPublisher<TestDbContext> domainEventsPublisher = new(_dbContext, _publisher);

        domainEventsPublisher.SaveChangesAndPublishDomainEvents();

        _publisher.Received(2).Publish(Arg.Any<ValueChangedDomainEvent>());
        _publisher.Context.Received(1).RemoveObject(SilverbackContextStorageExtensions.StorageTransactionObjectTypeId);
        _publisher.Context.Received(1).AddObject(SilverbackContextStorageExtensions.StorageTransactionObjectTypeId, storageTransaction);
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

    [Fact]
    public void SaveChangesAndPublishDomainEvents_ShouldClearAndRestoreStorageTransaction_WhenExistingAndStorageTransactionMismatch()
    {
        IStorageTransaction storageTransaction = Substitute.For<IStorageTransaction>();
        _publisher.Context.TryGetObject(SilverbackContextStorageExtensions.StorageTransactionObjectTypeId, out Arg.Any<IStorageTransaction?>())
            .Returns(callInfo =>
            {
                callInfo[1] = storageTransaction;
                return true;
            });

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
        _publisher.Context.Received(2).RemoveObject(SilverbackContextStorageExtensions.StorageTransactionObjectTypeId);
        _publisher.Context.Received(2).AddObject(SilverbackContextStorageExtensions.StorageTransactionObjectTypeId, Arg.Any<IStorageTransaction>());
        _publisher.Context.Received(1).AddObject(SilverbackContextStorageExtensions.StorageTransactionObjectTypeId, storageTransaction);
        _dbContext.ChangeTracker.Entries().ShouldAllBe(entry => entry.State == EntityState.Unchanged);
        _assertDbContext.TestDomainEntities.Count().ShouldBe(2);
        _assertDbContext.TestDomainEntities.ShouldAllBe(entry => entry.Value == 0);
    }

    [Fact]
    public void SaveChangesAndPublishDomainEvents_ShouldPublishEventsAndStoreEntities_WhenExistingAndStorageTransactionMatch()
    {
        IStorageTransaction storageTransaction = Substitute.For<IStorageTransaction>();
        _publisher.Context.TryGetObject(SilverbackContextStorageExtensions.StorageTransactionObjectTypeId, out Arg.Any<IStorageTransaction?>())
            .Returns(callInfo =>
            {
                callInfo[1] = storageTransaction;
                return true;
            });

        TestDomainEntity entity1 = _dbContext.TestDomainEntities.Add(new TestDomainEntity()).Entity;
        TestDomainEntity entity2 = _dbContext.TestDomainEntities.Add(new TestDomainEntity()).Entity;
        _dbContext.SaveChanges();

        entity1.SetValue(42);
        entity2.SetValue(42);

        EntityFrameworkDomainEventsPublisher<TestDbContext> domainEventsPublisher = new(_dbContext, _publisher);

        using IDbContextTransaction transaction = _dbContext.Database.BeginTransaction();
        storageTransaction.UnderlyingTransaction.Returns(transaction.GetDbTransaction());

        domainEventsPublisher.SaveChangesAndPublishDomainEvents();

        transaction.Rollback();

        _publisher.Received(2).Publish(Arg.Any<ValueChangedDomainEvent>());
        _publisher.Context.Received(0).RemoveObject(SilverbackContextStorageExtensions.StorageTransactionObjectTypeId);
        _publisher.Context.Received(0).AddObject(SilverbackContextStorageExtensions.StorageTransactionObjectTypeId, Arg.Any<IStorageTransaction>());
        _dbContext.ChangeTracker.Entries().ShouldAllBe(entry => entry.State == EntityState.Unchanged);
        _assertDbContext.TestDomainEntities.Count().ShouldBe(2);
        _assertDbContext.TestDomainEntities.ShouldAllBe(entry => entry.Value == 0);
    }
}
