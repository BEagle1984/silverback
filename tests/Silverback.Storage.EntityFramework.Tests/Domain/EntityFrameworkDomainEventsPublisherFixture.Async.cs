// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

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
    public async Task SaveChangesAndPublishDomainEventsAsync_ShouldClearAndRestoreStorageTransaction_WhenContextHasNoTransaction()
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
        await _dbContext.SaveChangesAsync();

        entity1.SetValue(42);
        entity2.SetValue(42);

        EntityFrameworkDomainEventsPublisher<TestDbContext> domainEventsPublisher = new(_dbContext, _publisher);

        await domainEventsPublisher.SaveChangesAndPublishDomainEventsAsync();

        await _publisher.Received(2).PublishAsync(Arg.Any<ValueChangedDomainEvent>());
        _publisher.Context.Received(1).RemoveObject(SilverbackContextStorageExtensions.StorageTransactionObjectTypeId);
        _publisher.Context.Received(1).AddObject(SilverbackContextStorageExtensions.StorageTransactionObjectTypeId, storageTransaction);
        _dbContext.ChangeTracker.Entries().ShouldAllBe(entry => entry.State == EntityState.Unchanged);
        _assertDbContext.TestDomainEntities.Count().ShouldBe(2);
        _assertDbContext.TestDomainEntities.ShouldAllBe(entry => entry.Value == 42);
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

    [Fact]
    public async Task SaveChangesAndPublishDomainEventsAsync_ShouldClearAndRestoreStorageTransaction_WhenExistingAndStorageTransactionMismatch()
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
        await _dbContext.SaveChangesAsync();

        entity1.SetValue(42);
        entity2.SetValue(42);

        EntityFrameworkDomainEventsPublisher<TestDbContext> domainEventsPublisher = new(_dbContext, _publisher);

        await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync();

        await domainEventsPublisher.SaveChangesAndPublishDomainEventsAsync();

        await transaction.RollbackAsync();

        await _publisher.Received(2).PublishAsync(Arg.Any<ValueChangedDomainEvent>());
        _publisher.Context.Received(2).RemoveObject(SilverbackContextStorageExtensions.StorageTransactionObjectTypeId);
        _publisher.Context.Received(2).AddObject(SilverbackContextStorageExtensions.StorageTransactionObjectTypeId, Arg.Any<IStorageTransaction>());
        _publisher.Context.Received(1).AddObject(SilverbackContextStorageExtensions.StorageTransactionObjectTypeId, storageTransaction);
        _dbContext.ChangeTracker.Entries().ShouldAllBe(entry => entry.State == EntityState.Unchanged);
        _assertDbContext.TestDomainEntities.Count().ShouldBe(2);
        _assertDbContext.TestDomainEntities.ShouldAllBe(entry => entry.Value == 0);
    }

    [Fact]
    public async Task SaveChangesAndPublishDomainEventsAsync_ShouldPublishEventsAndStoreEntities_WhenExistingAndStorageTransactionMatch()
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
        await _dbContext.SaveChangesAsync();

        entity1.SetValue(42);
        entity2.SetValue(42);

        EntityFrameworkDomainEventsPublisher<TestDbContext> domainEventsPublisher = new(_dbContext, _publisher);

        await using IDbContextTransaction transaction = await _dbContext.Database.BeginTransactionAsync();
        storageTransaction.UnderlyingTransaction.Returns(transaction.GetDbTransaction());

        await domainEventsPublisher.SaveChangesAndPublishDomainEventsAsync();

        await transaction.RollbackAsync();

        await _publisher.Received(2).PublishAsync(Arg.Any<ValueChangedDomainEvent>());
        _publisher.Context.Received(0).RemoveObject(SilverbackContextStorageExtensions.StorageTransactionObjectTypeId);
        _publisher.Context.Received(0).AddObject(SilverbackContextStorageExtensions.StorageTransactionObjectTypeId, Arg.Any<IStorageTransaction>());
        _dbContext.ChangeTracker.Entries().ShouldAllBe(entry => entry.State == EntityState.Unchanged);
        _assertDbContext.TestDomainEntities.Count().ShouldBe(2);
        _assertDbContext.TestDomainEntities.ShouldAllBe(entry => entry.Value == 0);
    }
}
