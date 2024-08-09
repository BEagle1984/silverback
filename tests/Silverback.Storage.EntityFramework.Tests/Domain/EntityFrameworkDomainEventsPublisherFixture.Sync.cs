// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NSubstitute;
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
        _dbContext.ChangeTracker.Entries().Should().AllSatisfy(entry => entry.State.Should().Be(EntityState.Unchanged));
        _assertDbContext.TestDomainEntities.Should().HaveCount(2);
        _assertDbContext.TestDomainEntities.Should().AllSatisfy(entity => entity.Value.Should().Be(42));
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
        _dbContext.ChangeTracker.Entries().Should().AllSatisfy(entry => entry.State.Should().Be(EntityState.Unchanged));
        _assertDbContext.TestDomainEntities.Should().HaveCount(2);
        _assertDbContext.TestDomainEntities.Should().AllSatisfy(entity => entity.Value.Should().Be(42));
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
        _dbContext.ChangeTracker.Entries().Should().AllSatisfy(entry => entry.State.Should().Be(EntityState.Unchanged));
        _assertDbContext.TestDomainEntities.Should().HaveCount(2);
        _assertDbContext.TestDomainEntities.Should().AllSatisfy(entity => entity.Value.Should().Be(0));
    }
}
