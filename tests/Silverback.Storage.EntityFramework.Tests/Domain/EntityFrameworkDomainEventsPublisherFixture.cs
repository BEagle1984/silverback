// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Silverback.Domain;
using Silverback.Messaging.Publishing;

namespace Silverback.Tests.Storage.EntityFramework.Domain;

public sealed partial class EntityFrameworkDomainEventsPublisherFixture : IDisposable
{
    private readonly SqliteConnection _sqliteConnection;

    private readonly TestDbContext _dbContext;

    private readonly TestDbContext _assertDbContext;

    private readonly IPublisher _publisher;

    public EntityFrameworkDomainEventsPublisherFixture()
    {
        _sqliteConnection = new SqliteConnection($"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        _sqliteConnection.Open();

        _dbContext = new TestDbContext(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(_sqliteConnection).Options);
        _dbContext.Database.EnsureCreated();

        _assertDbContext = new TestDbContext(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(_sqliteConnection).Options);

        _publisher = Substitute.For<IPublisher>();
        _publisher.Context.Returns(Substitute.For<ISilverbackContext>());
    }

    public void Dispose() => _sqliteConnection.Dispose();

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options)
        {
        }

        [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local", Justification = "Required by EF Core")]
        public DbSet<TestDomainEntity> TestDomainEntities { get; set; } = null!;
    }

    private class TestDomainEntity : DomainEntity
    {
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Required by EF Core")]
        public int Id { get; private set; }

        public int Value { get; private set; }

        public void SetValue(int newValue)
        {
            Value = newValue;
            AddEvent<ValueChangedDomainEvent>();
        }
    }

    private class ValueChangedDomainEvent : DomainEvent<TestDomainEntity>;
}
