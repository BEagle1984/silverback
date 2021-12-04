// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Silverback.Database;
using Silverback.EventStore;
using Silverback.Tests.EventSourcing.TestTypes;
using Xunit;

namespace Silverback.Tests.EventSourcing.EventStore;

public sealed class DbEventStoreRepositoryTests : IDisposable
{
    private readonly TestDbContext _dbContext;

    private readonly SqliteConnection _connection;

    public DbEventStoreRepositoryTests()
    {
        _connection = new SqliteConnection($"Data Source={Guid.NewGuid():N};Mode=Memory;Cache=Shared");
        _connection.Open();

        _dbContext = new TestDbContext(new DbContextOptionsBuilder().UseSqlite(_connection.ConnectionString).Options);
        _dbContext.Database.EnsureCreated();

        SilverbackQueryableExtensions.Implementation = new EfCoreQueryableExtensions();
    }

    [Fact]
    public void Store_EntityWithSomeEvents_EventsSaved()
    {
        PersonDbEventStoreRepository repo = new(new EfCoreDbContext<TestDbContext>(_dbContext));

        Person person = new();
        person.ChangeName("Sergio");
        person.ChangeAge(35);

        repo.Store(person);
        _dbContext.SaveChanges();

        _dbContext.Persons.Should().HaveCount(1);
        _dbContext.Persons.First().Events.Should().HaveCount(2);
    }

    [Fact]
    public async Task StoreAsync_EntityWithSomeEvents_EventsSaved()
    {
        PersonDbEventStoreRepository repo = new(new EfCoreDbContext<TestDbContext>(_dbContext));

        Person person = new();
        person.ChangeName("Sergio");
        person.ChangeAge(35);

        await repo.StoreAsync(person);
        await _dbContext.SaveChangesAsync();

        _dbContext.Persons.Should().HaveCount(1);
        _dbContext.Persons.First().Events.Should().HaveCount(2);
    }

    [Fact]
    public void Store_ExistingEntity_NewEventsSaved()
    {
        PersonEventStore? eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12, EntityVersion = 1 }).Entity;
        eventStore.Events.Add(
            new PersonEvent
            {
                SerializedEvent = "{\"NewName\": \"Silverback\"}",
                ClrType =
                    "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests"
            });
        _dbContext.SaveChanges();

        PersonDbEventStoreRepository repo = new(new EfCoreDbContext<TestDbContext>(_dbContext));

        Person? person = repo.Find(p => p.Id == 12);

        person!.ChangeName("Sergio");
        person.ChangeAge(35);

        repo.Store(person);
        _dbContext.SaveChanges();

        _dbContext.Persons.Should().HaveCount(1);
        _dbContext.Persons.First().Events.Should().HaveCount(3);
    }

    [Fact]
    public async Task StoreAsync_ExistingEntity_NewEventsSaved()
    {
        PersonEventStore? eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12, EntityVersion = 1 }).Entity;
        eventStore.Events.Add(
            new PersonEvent
            {
                SerializedEvent = "{\"NewName\": \"Silverback\"}",
                ClrType =
                    "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests"
            });
        _dbContext.SaveChanges();

        PersonDbEventStoreRepository repo = new(new EfCoreDbContext<TestDbContext>(_dbContext));

        Person? person = await repo.FindAsync(p => p.Id == 12);

        person!.ChangeName("Sergio");
        person.ChangeAge(35);

        await repo.StoreAsync(person);
        await _dbContext.SaveChangesAsync();

        _dbContext.Persons.Should().HaveCount(1);
        _dbContext.Persons.First().Events.Should().HaveCount(3);
    }

    [Fact]
    public void Store_EntityWithSomeEvents_VersionCalculated()
    {
        PersonDbEventStoreRepository repo = new(new EfCoreDbContext<TestDbContext>(_dbContext));
        Person person = new();
        person.ChangeName("Sergio");
        person.ChangeAge(35);

        repo.Store(person);
        _dbContext.SaveChanges();

        _dbContext.Persons.First().EntityVersion.Should().Be(2);
    }

    [Fact]
    public async Task StoreAsync_EntityWithSomeEvents_VersionCalculated()
    {
        PersonDbEventStoreRepository repo = new(new EfCoreDbContext<TestDbContext>(_dbContext));
        Person person = new();
        person.ChangeName("Sergio");
        person.ChangeAge(35);

        await repo.StoreAsync(person);
        await _dbContext.SaveChangesAsync();

        _dbContext.Persons.First().EntityVersion.Should().Be(2);
    }

    [Fact]
    public void Store_ExistingEntity_VersionIncremented()
    {
        PersonEventStore? eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12, EntityVersion = 1 }).Entity;
        eventStore.Events.Add(
            new PersonEvent
            {
                SerializedEvent = "{\"NewName\": \"Silverback\"}",
                ClrType =
                    "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests"
            });
        _dbContext.SaveChanges();

        PersonDbEventStoreRepository repo = new(new EfCoreDbContext<TestDbContext>(_dbContext));

        Person? person = repo.Find(p => p.Id == 12);

        person!.ChangeName("Sergio");
        person.ChangeAge(35);

        repo.Store(person);

        _dbContext.Persons.First().EntityVersion.Should().Be(3);
    }

    [Fact]
    public async Task StoreAsync_ExistingEntity_VersionIncremented()
    {
        PersonEventStore? eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12, EntityVersion = 1 }).Entity;
        eventStore.Events.Add(
            new PersonEvent
            {
                SerializedEvent = "{\"NewName\": \"Silverback\"}",
                ClrType =
                    "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests"
            });
        _dbContext.SaveChanges();

        PersonDbEventStoreRepository repo = new(new EfCoreDbContext<TestDbContext>(_dbContext));

        Person? person = repo.Find(p => p.Id == 12);

        person!.ChangeName("Sergio");
        person.ChangeAge(35);

        await repo.StoreAsync(person);

        _dbContext.Persons.First().EntityVersion.Should().Be(3);
    }

    [Fact]
    public void Store_ConcurrentlyModifyExistingEntity_ExceptionThrown()
    {
        PersonEventStore? eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12, EntityVersion = 1 }).Entity;
        eventStore.Events.Add(
            new PersonEvent
            {
                SerializedEvent = "{\"NewName\": \"Silverback\"}",
                ClrType =
                    "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests"
            });
        _dbContext.SaveChanges();

        PersonDbEventStoreRepository repo = new(new EfCoreDbContext<TestDbContext>(_dbContext));

        Person? person = repo.Find(p => p.Id == 12);
        Person? person2 = repo.Find(p => p.Id == 12);

        person!.ChangeName("Sergio");
        person.ChangeAge(35);
        person2!.ChangeName("Sergio");
        person2.ChangeAge(35);

        repo.Store(person);
        Action act = () => repo.Store(person2);

        act.Should().Throw<EventStoreConcurrencyException>();
    }

    [Fact]
    public async Task StoreAsync_ConcurrentlyModifyExistingEntity_ExceptionThrown()
    {
        PersonEventStore? eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12, EntityVersion = 1 }).Entity;
        eventStore.Events.Add(
            new PersonEvent
            {
                SerializedEvent = "{\"NewName\": \"Silverback\"}",
                ClrType =
                    "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests"
            });
        _dbContext.SaveChanges();

        PersonDbEventStoreRepository repo = new(new EfCoreDbContext<TestDbContext>(_dbContext));

        Person? person = repo.Find(p => p.Id == 12);
        Person? person2 = repo.Find(p => p.Id == 12);

        person!.ChangeName("Sergio");
        person.ChangeAge(35);
        person2!.ChangeName("Sergio");
        person2.ChangeAge(35);

        await repo.StoreAsync(person);
        Func<Task> act = async () => await repo.StoreAsync(person2);

        await act.Should().ThrowAsync<EventStoreConcurrencyException>();
    }

    [Fact]
    public void Find_ExistingId_EntityRecreated()
    {
        PersonEventStore? eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12 }).Entity;
        eventStore.Events.Add(
            new PersonEvent
            {
                SerializedEvent = "{\"NewName\": \"Silverback\"}",
                ClrType =
                    "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests"
            });
        _dbContext.SaveChanges();

        PersonDbEventStoreRepository repo = new(new EfCoreDbContext<TestDbContext>(_dbContext));

        Person? entity = repo.Find(p => p.Id == 12);

        entity.Should().NotBe(null);
    }

    [Fact]
    public void Find_ExistingId_EventsApplied()
    {
        PersonEventStore? eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12 }).Entity;
        eventStore.Events.Add(
            new PersonEvent
            {
                SerializedEvent = "{\"NewName\": \"Silverback\"}",
                ClrType =
                    "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests"
            });
        eventStore.Events.Add(
            new PersonEvent
            {
                SerializedEvent = "{\"NewAge\": 35}",
                ClrType =
                    "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.AgeChangedEvent, Silverback.EventSourcing.Tests"
            });

        _dbContext.SaveChanges();

        PersonDbEventStoreRepository repo = new(new EfCoreDbContext<TestDbContext>(_dbContext));

        Person? entity = repo.Find(p => p.Id == 12);

        entity.Should().NotBeNull();
        entity!.Name.Should().Be("Silverback");
        entity.Age.Should().Be(35);
    }

    [Fact]
    public void Find_ExistingId_EventsAppliedInRightOrder()
    {
        PersonEventStore? eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12 }).Entity;
        eventStore.Events.Add(
            new PersonEvent
            {
                SerializedEvent = "{\"NewName\": \"Silverback\"}",
                ClrType =
                    "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests",
                Timestamp = DateTime.Parse("2000-05-05", CultureInfo.InvariantCulture)
            });
        eventStore.Events.Add(
            new PersonEvent
            {
                SerializedEvent = "{\"NewName\": \"Sergio\"}",
                ClrType =
                    "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests",
                Timestamp = DateTime.Parse("2000-03-01", CultureInfo.InvariantCulture)
            });

        _dbContext.SaveChanges();

        PersonDbEventStoreRepository repo = new(new EfCoreDbContext<TestDbContext>(_dbContext));

        Person? entity = repo.Find(p => p.Id == 12);

        entity.Should().NotBeNull();
        entity!.Name.Should().Be("Silverback");
    }

    [Fact]
    public void Find_EventsWithLegacySerialization_EventsApplied()
    {
        PersonEventStore? eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12 }).Entity;
        eventStore.Events.Add(
            new PersonEvent
            {
                SerializedEvent = "{" +
                                  "\"$type\": \"Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests\"," +
                                  "\"NewName\": \"Silverback\"" +
                                  "}"
            });
        eventStore.Events.Add(
            new PersonEvent
            {
                SerializedEvent = "{" +
                                  "\"$type\": \"Silverback.Tests.EventSourcing.TestTypes.EntityEvents.AgeChangedEvent, Silverback.EventSourcing.Tests\"," +
                                  "\"NewAge\": 35" +
                                  "}"
            });

        _dbContext.SaveChanges();

        PersonDbEventStoreRepository repo = new(new EfCoreDbContext<TestDbContext>(_dbContext));

        Person? entity = repo.Find(p => p.Id == 12);

        entity.Should().NotBeNull();
        entity!.Name.Should().Be("Silverback");
        entity.Age.Should().Be(35);
    }

    [Fact]
    public void Find_NonExistingId_NullReturned()
    {
        PersonDbEventStoreRepository repo = new(new EfCoreDbContext<TestDbContext>(_dbContext));

        Person? entity = repo.Find(p => p.Id == 12);

        entity.Should().BeNull();
    }

    [Fact]
    public async Task FindAsync_ExistingId_EntityRecreated()
    {
        PersonEventStore? eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12 }).Entity;
        eventStore.Events.Add(
            new PersonEvent
            {
                SerializedEvent = "{\"NewName\": \"Silverback\"}",
                ClrType =
                    "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests"
            });
        _dbContext.SaveChanges();

        PersonDbEventStoreRepository repo = new(new EfCoreDbContext<TestDbContext>(_dbContext));

        Person? entity = await repo.FindAsync(p => p.Id == 12);

        entity.Should().NotBeNull();
    }

    [Fact]
    public async Task FindAsync_ExistingId_EventsApplied()
    {
        PersonEventStore? eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12 }).Entity;
        eventStore.Events.Add(
            new PersonEvent
            {
                SerializedEvent = "{\"NewName\": \"Silverback\"}",
                ClrType =
                    "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests"
            });
        eventStore.Events.Add(
            new PersonEvent
            {
                SerializedEvent = "{\"NewAge\": 35}",
                ClrType =
                    "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.AgeChangedEvent, Silverback.EventSourcing.Tests"
            });

        _dbContext.SaveChanges();

        PersonDbEventStoreRepository repo = new(new EfCoreDbContext<TestDbContext>(_dbContext));

        Person? entity = await repo.FindAsync(p => p.Id == 12);

        entity.Should().NotBeNull();
        entity!.Name.Should().Be("Silverback");
        entity.Age.Should().Be(35);
    }

    [Fact]
    public async Task FindAsync_ExistingId_EventsAppliedInRightOrder()
    {
        PersonEventStore? eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12 }).Entity;
        eventStore.Events.Add(
            new PersonEvent
            {
                SerializedEvent = "{\"NewName\": \"Silverback\"}",
                ClrType =
                    "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests",
                Timestamp = DateTime.Parse("2000-05-05", CultureInfo.InvariantCulture)
            });
        eventStore.Events.Add(
            new PersonEvent
            {
                SerializedEvent = "{\"NewName\": \"Sergio\"}",
                ClrType =
                    "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests",
                Timestamp = DateTime.Parse("2000-03-01", CultureInfo.InvariantCulture)
            });

        _dbContext.SaveChanges();

        PersonDbEventStoreRepository repo = new(new EfCoreDbContext<TestDbContext>(_dbContext));

        Person? entity = await repo.FindAsync(p => p.Id == 12);

        entity.Should().NotBeNull();
        entity!.Name.Should().Be("Silverback");
    }

    [Fact]
    public async Task FindAsync_EventsWithLegacySerialization_EventsApplied()
    {
        PersonEventStore? eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12 }).Entity;
        eventStore.Events.Add(
            new PersonEvent
            {
                SerializedEvent = "{" +
                                  "\"$type\": \"Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests\"," +
                                  "\"NewName\": \"Silverback\"" +
                                  "}"
            });
        eventStore.Events.Add(
            new PersonEvent
            {
                SerializedEvent = "{" +
                                  "\"$type\": \"Silverback.Tests.EventSourcing.TestTypes.EntityEvents.AgeChangedEvent, Silverback.EventSourcing.Tests\"," +
                                  "\"NewAge\": 35" +
                                  "}"
            });

        _dbContext.SaveChanges();

        PersonDbEventStoreRepository repo = new(new EfCoreDbContext<TestDbContext>(_dbContext));

        Person? entity = await repo.FindAsync(p => p.Id == 12);

        entity.Should().NotBeNull();
        entity!.Name.Should().Be("Silverback");
        entity.Age.Should().Be(35);
    }

    [Fact]
    public async Task FindAsync_NonExistingId_NullReturned()
    {
        PersonDbEventStoreRepository repo = new(new EfCoreDbContext<TestDbContext>(_dbContext));

        Person? entity = await repo.FindAsync(p => p.Id == 12);

        entity.Should().BeNull();
    }

    [Fact]
    public void Find_ExistingIdWithPastSnapshot_OnlyRelevantEventsApplied()
    {
        PersonEventStore? eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12 }).Entity;
        eventStore.Events.Add(
            new PersonEvent
            {
                SerializedEvent = "{\"NewName\": \"Silverback\"}",
                ClrType =
                    "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests",
                Timestamp = DateTime.Parse("2000-05-05", CultureInfo.InvariantCulture)
            });
        eventStore.Events.Add(
            new PersonEvent
            {
                SerializedEvent = "{\"NewName\": \"Sergio\"}",
                ClrType =
                    "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests",
                Timestamp = DateTime.Parse("2000-03-01", CultureInfo.InvariantCulture)
            });
        eventStore.Events.Add(
            new PersonEvent
            {
                SerializedEvent = "{\"NewAge\": 16}",
                ClrType =
                    "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.AgeChangedEvent, Silverback.EventSourcing.Tests",
                Timestamp = DateTime.Parse("2000-02-01", CultureInfo.InvariantCulture)
            });
        eventStore.Events.Add(
            new PersonEvent
            {
                SerializedEvent = "{\"NewAge\": 35}",
                ClrType =
                    "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.AgeChangedEvent, Silverback.EventSourcing.Tests",
                Timestamp = DateTime.Parse("2019-07-06", CultureInfo.InvariantCulture)
            });

        _dbContext.SaveChanges();

        PersonDbEventStoreRepository repo = new(new EfCoreDbContext<TestDbContext>(_dbContext));

        Person? entity = repo.Find(p => p.Id == 12, DateTime.Parse("2000-03-01", CultureInfo.InvariantCulture));

        entity.Should().NotBeNull();
        entity!.Name.Should().Be("Sergio");
        entity.Age.Should().Be(16);
    }

    [Fact]
    public async Task FindAsync_ExistingIdWithPastSnapshot_OnlyRelevantEventsApplied()
    {
        PersonEventStore? eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12 }).Entity;
        eventStore.Events.Add(
            new PersonEvent
            {
                SerializedEvent = "{\"NewName\": \"Silverback\"}",
                ClrType =
                    "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests",
                Timestamp = DateTime.Parse("2000-05-05", CultureInfo.InvariantCulture)
            });
        eventStore.Events.Add(
            new PersonEvent
            {
                SerializedEvent = "{\"NewName\": \"Sergio\"}",
                ClrType =
                    "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests",
                Timestamp = DateTime.Parse("2000-03-01", CultureInfo.InvariantCulture)
            });
        eventStore.Events.Add(
            new PersonEvent
            {
                SerializedEvent = "{\"NewAge\": 16}",
                ClrType =
                    "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.AgeChangedEvent, Silverback.EventSourcing.Tests",
                Timestamp = DateTime.Parse("2000-02-01", CultureInfo.InvariantCulture)
            });
        eventStore.Events.Add(
            new PersonEvent
            {
                SerializedEvent = "{\"NewAge\": 35}",
                ClrType =
                    "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.AgeChangedEvent, Silverback.EventSourcing.Tests",
                Timestamp = DateTime.Parse("2019-07-06", CultureInfo.InvariantCulture)
            });

        _dbContext.SaveChanges();

        PersonDbEventStoreRepository repo = new(new EfCoreDbContext<TestDbContext>(_dbContext));

        Person? entity = await repo.FindAsync(
            p => p.Id == 12,
            DateTime.Parse("2000-03-01", CultureInfo.InvariantCulture));

        entity.Should().NotBeNull();
        entity!.Name.Should().Be("Sergio");
        entity.Age.Should().Be(16);
    }

    [Fact]
    public void Remove_ExistingEntity_EntityDeleted()
    {
        PersonEventStore? eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12 }).Entity;
        eventStore.Events.Add(
            new PersonEvent
            {
                SerializedEvent = "{\"NewName\": \"Silverback\"}",
                ClrType =
                    "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests"
            });
        _dbContext.SaveChanges();

        PersonDbEventStoreRepository repo = new(new EfCoreDbContext<TestDbContext>(_dbContext));

        Person? entity = repo.Find(p => p.Id == 12);
        entity.Should().NotBeNull();

        repo.Remove(entity!);
        _dbContext.SaveChanges();

        _dbContext.Persons.Should().BeEmpty();
        _dbContext.Persons.SelectMany(s => s.Events).Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveAsync_ExistingEntity_EntityDeleted()
    {
        PersonEventStore? eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12 }).Entity;
        eventStore.Events.Add(
            new PersonEvent
            {
                SerializedEvent = "{\"NewName\": \"Silverback\"}",
                ClrType =
                    "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests"
            });
        _dbContext.SaveChanges();

        PersonDbEventStoreRepository repo = new(new EfCoreDbContext<TestDbContext>(_dbContext));

        Person? entity = await repo.FindAsync(p => p.Id == 12);
        entity.Should().NotBeNull();

        await repo.RemoveAsync(entity!);
        await _dbContext.SaveChangesAsync();

        _dbContext.Persons.Should().BeEmpty();
        _dbContext.Persons.SelectMany(s => s.Events).Should().BeEmpty();
    }

    [Fact]
    public void Remove_NonExistingEntity_ExceptionThrown()
    {
        PersonDbEventStoreRepository repo = new(new EfCoreDbContext<TestDbContext>(_dbContext));

        Person entity = new(123);

        Action act = () => repo.Remove(entity);

        act.Should().Throw<EventStoreNotFoundException>();
    }

    [Fact]
    public async Task RemoveAsync_NonExistingEntity_ExceptionThrown()
    {
        PersonDbEventStoreRepository repo = new(new EfCoreDbContext<TestDbContext>(_dbContext));

        Person entity = new(123);

        Func<Task> act = () => repo.RemoveAsync(entity);

        await act.Should().ThrowAsync<EventStoreNotFoundException>();
    }

    public void Dispose()
    {
        _connection.SafeClose();
        _connection.Dispose();
        _dbContext.Dispose();
    }
}
