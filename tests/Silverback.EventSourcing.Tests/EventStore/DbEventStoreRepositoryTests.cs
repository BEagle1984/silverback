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

namespace Silverback.Tests.EventSourcing.EventStore
{
    public sealed class DbEventStoreRepositoryTests : IAsyncDisposable
    {
        private readonly TestDbContext _dbContext;

        private readonly SqliteConnection _connection;

        public DbEventStoreRepositoryTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _dbContext = new TestDbContext(new DbContextOptionsBuilder().UseSqlite(_connection).Options);
            _dbContext.Database.EnsureCreated();

            SilverbackQueryableExtensions.Implementation = new EfCoreQueryableExtensions();
        }

        [Fact]
        public void Store_EntityWithSomeEvents_EventsSaved()
        {
            var repo = new PersonDbEventStoreRepository(new EfCoreDbContext<TestDbContext>(_dbContext));

            var person = new Person();
            person.ChangeName("Sergio");
            person.ChangeAge(35);

            repo.Store(person);
            _dbContext.SaveChanges();

            _dbContext.Persons.Count().Should().Be(1);
            _dbContext.Persons.First().Events.Count.Should().Be(2);
        }

        [Fact]
        public async Task StoreAsync_EntityWithSomeEvents_EventsSaved()
        {
            var repo = new PersonDbEventStoreRepository(new EfCoreDbContext<TestDbContext>(_dbContext));

            var person = new Person();
            person.ChangeName("Sergio");
            person.ChangeAge(35);

            await repo.StoreAsync(person);
            await _dbContext.SaveChangesAsync();

            _dbContext.Persons.Count().Should().Be(1);
            _dbContext.Persons.First().Events.Count.Should().Be(2);
        }

        [Fact]
        public void Store_ExistingEntity_NewEventsSaved()
        {
            var eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12, EntityVersion = 1 }).Entity;
            eventStore.Events.Add(
                new PersonEvent
                {
                    SerializedEvent = "{\"NewName\": \"Silverback\"}",
                    ClrType =
                        "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests"
                });
            _dbContext.SaveChanges();

            var repo = new PersonDbEventStoreRepository(new EfCoreDbContext<TestDbContext>(_dbContext));

            var person = repo.Find(p => p.Id == 12);

            person!.ChangeName("Sergio");
            person!.ChangeAge(35);

            repo.Store(person);
            _dbContext.SaveChanges();

            _dbContext.Persons.Count().Should().Be(1);
            _dbContext.Persons.First().Events.Count.Should().Be(3);
        }

        [Fact]
        public async Task StoreAsync_ExistingEntity_NewEventsSaved()
        {
            var eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12, EntityVersion = 1 }).Entity;
            eventStore.Events.Add(
                new PersonEvent
                {
                    SerializedEvent = "{\"NewName\": \"Silverback\"}",
                    ClrType =
                        "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests"
                });
            _dbContext.SaveChanges();

            var repo = new PersonDbEventStoreRepository(new EfCoreDbContext<TestDbContext>(_dbContext));

            var person = await repo.FindAsync(p => p.Id == 12);

            person!.ChangeName("Sergio");
            person!.ChangeAge(35);

            await repo.StoreAsync(person);
            await _dbContext.SaveChangesAsync();

            _dbContext.Persons.Count().Should().Be(1);
            _dbContext.Persons.First().Events.Count.Should().Be(3);
        }

        [Fact]
        public void Store_EntityWithSomeEvents_VersionCalculated()
        {
            var repo = new PersonDbEventStoreRepository(new EfCoreDbContext<TestDbContext>(_dbContext));
            var person = new Person();
            person.ChangeName("Sergio");
            person.ChangeAge(35);

            repo.Store(person);
            _dbContext.SaveChanges();

            _dbContext.Persons.First().EntityVersion.Should().Be(2);
        }

        [Fact]
        public async Task StoreAsync_EntityWithSomeEvents_VersionCalculated()
        {
            var repo = new PersonDbEventStoreRepository(new EfCoreDbContext<TestDbContext>(_dbContext));
            var person = new Person();
            person.ChangeName("Sergio");
            person.ChangeAge(35);

            await repo.StoreAsync(person);
            await _dbContext.SaveChangesAsync();

            _dbContext.Persons.First().EntityVersion.Should().Be(2);
        }

        [Fact]
        public void Store_ExistingEntity_VersionIncremented()
        {
            var eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12, EntityVersion = 1 }).Entity;
            eventStore.Events.Add(
                new PersonEvent
                {
                    SerializedEvent = "{\"NewName\": \"Silverback\"}",
                    ClrType =
                        "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests"
                });
            _dbContext.SaveChanges();

            var repo = new PersonDbEventStoreRepository(new EfCoreDbContext<TestDbContext>(_dbContext));

            var person = repo.Find(p => p.Id == 12);

            person!.ChangeName("Sergio");
            person!.ChangeAge(35);

            repo.Store(person);

            _dbContext.Persons.First().EntityVersion.Should().Be(3);
        }

        [Fact]
        public async Task StoreAsync_ExistingEntity_VersionIncremented()
        {
            var eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12, EntityVersion = 1 }).Entity;
            eventStore.Events.Add(
                new PersonEvent
                {
                    SerializedEvent = "{\"NewName\": \"Silverback\"}",
                    ClrType =
                        "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests"
                });
            _dbContext.SaveChanges();

            var repo = new PersonDbEventStoreRepository(new EfCoreDbContext<TestDbContext>(_dbContext));

            var person = repo.Find(p => p.Id == 12);

            person!.ChangeName("Sergio");
            person!.ChangeAge(35);

            await repo.StoreAsync(person);

            _dbContext.Persons.First().EntityVersion.Should().Be(3);
        }

        [Fact]
        public void Store_ConcurrentlyModifyExistingEntity_ExceptionThrown()
        {
            var eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12, EntityVersion = 1 }).Entity;
            eventStore.Events.Add(
                new PersonEvent
                {
                    SerializedEvent = "{\"NewName\": \"Silverback\"}",
                    ClrType =
                        "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests"
                });
            _dbContext.SaveChanges();

            var repo = new PersonDbEventStoreRepository(new EfCoreDbContext<TestDbContext>(_dbContext));

            var person = repo.Find(p => p.Id == 12);
            var person2 = repo.Find(p => p.Id == 12);

            person!.ChangeName("Sergio");
            person!.ChangeAge(35);
            person2!.ChangeName("Sergio");
            person2!.ChangeAge(35);

            repo.Store(person);
            Action act = () => repo.Store(person2);

            act.Should().Throw<EventStoreConcurrencyException>();
        }

        [Fact]
        public async Task StoreAsync_ConcurrentlyModifyExistingEntity_ExceptionThrown()
        {
            var eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12, EntityVersion = 1 }).Entity;
            eventStore.Events.Add(
                new PersonEvent
                {
                    SerializedEvent = "{\"NewName\": \"Silverback\"}",
                    ClrType =
                        "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests"
                });
            _dbContext.SaveChanges();

            var repo = new PersonDbEventStoreRepository(new EfCoreDbContext<TestDbContext>(_dbContext));

            var person = repo.Find(p => p.Id == 12);
            var person2 = repo.Find(p => p.Id == 12);

            person!.ChangeName("Sergio");
            person!.ChangeAge(35);
            person2!.ChangeName("Sergio");
            person2!.ChangeAge(35);

            await repo.StoreAsync(person);
            Func<Task> act = async () => await repo.StoreAsync(person2);

            act.Should().Throw<EventStoreConcurrencyException>();
        }

        [Fact]
        public void Find_ExistingId_EntityRecreated()
        {
            var eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12 }).Entity;
            eventStore.Events.Add(
                new PersonEvent
                {
                    SerializedEvent = "{\"NewName\": \"Silverback\"}",
                    ClrType =
                        "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests"
                });
            _dbContext.SaveChanges();

            var repo = new PersonDbEventStoreRepository(new EfCoreDbContext<TestDbContext>(_dbContext));

            var entity = repo.Find(p => p.Id == 12);

            entity.Should().NotBe(null);
        }

        [Fact]
        public void Find_ExistingId_EventsApplied()
        {
            var eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12 }).Entity;
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

            var repo = new PersonDbEventStoreRepository(new EfCoreDbContext<TestDbContext>(_dbContext));

            var entity = repo.Find(p => p.Id == 12);

            entity.Should().NotBeNull();
            entity!.Name.Should().Be("Silverback");
            entity!.Age.Should().Be(35);
        }

        [Fact]
        public void Find_ExistingId_EventsAppliedInRightOrder()
        {
            var eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12 }).Entity;
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

            var repo = new PersonDbEventStoreRepository(new EfCoreDbContext<TestDbContext>(_dbContext));

            var entity = repo.Find(p => p.Id == 12);

            entity.Should().NotBeNull();
            entity!.Name.Should().Be("Silverback");
        }

        [Fact]
        public void Find_EventsWithLegacySerialization_EventsApplied()
        {
            var eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12 }).Entity;
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

            var repo = new PersonDbEventStoreRepository(new EfCoreDbContext<TestDbContext>(_dbContext));

            var entity = repo.Find(p => p.Id == 12);

            entity.Should().NotBeNull();
            entity!.Name.Should().Be("Silverback");
            entity!.Age.Should().Be(35);
        }

        [Fact]
        public void Find_NonExistingId_NullReturned()
        {
            var repo = new PersonDbEventStoreRepository(new EfCoreDbContext<TestDbContext>(_dbContext));

            var entity = repo.Find(p => p.Id == 12);

            entity.Should().BeNull();
        }

        [Fact]
        public async Task FindAsync_ExistingId_EntityRecreated()
        {
            var eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12 }).Entity;
            eventStore.Events.Add(
                new PersonEvent
                {
                    SerializedEvent = "{\"NewName\": \"Silverback\"}",
                    ClrType =
                        "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests"
                });
            _dbContext.SaveChanges();

            var repo = new PersonDbEventStoreRepository(new EfCoreDbContext<TestDbContext>(_dbContext));

            var entity = await repo.FindAsync(p => p.Id == 12);

            entity.Should().NotBeNull();
        }

        [Fact]
        public async Task FindAsync_ExistingId_EventsApplied()
        {
            var eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12 }).Entity;
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

            var repo = new PersonDbEventStoreRepository(new EfCoreDbContext<TestDbContext>(_dbContext));

            var entity = await repo.FindAsync(p => p.Id == 12);

            entity.Should().NotBeNull();
            entity!.Name.Should().Be("Silverback");
            entity!.Age.Should().Be(35);
        }

        [Fact]
        public async Task FindAsync_ExistingId_EventsAppliedInRightOrder()
        {
            var eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12 }).Entity;
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

            var repo = new PersonDbEventStoreRepository(new EfCoreDbContext<TestDbContext>(_dbContext));

            var entity = await repo.FindAsync(p => p.Id == 12);

            entity.Should().NotBeNull();
            entity!.Name.Should().Be("Silverback");
        }

        [Fact]
        public async Task FindAsync_EventsWithLegacySerialization_EventsApplied()
        {
            var eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12 }).Entity;
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

            var repo = new PersonDbEventStoreRepository(new EfCoreDbContext<TestDbContext>(_dbContext));

            var entity = await repo.FindAsync(p => p.Id == 12);

            entity.Should().NotBeNull();
            entity!.Name.Should().Be("Silverback");
            entity!.Age.Should().Be(35);
        }

        [Fact]
        public async Task FindAsync_NonExistingId_NullReturned()
        {
            var repo = new PersonDbEventStoreRepository(new EfCoreDbContext<TestDbContext>(_dbContext));

            var entity = await repo.FindAsync(p => p.Id == 12);

            entity.Should().BeNull();
        }

        [Fact]
        public void Find_ExistingIdWithPastSnapshot_OnlyRelevantEventsApplied()
        {
            var eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12 }).Entity;
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

            var repo = new PersonDbEventStoreRepository(new EfCoreDbContext<TestDbContext>(_dbContext));

            var entity = repo.Find(p => p.Id == 12, DateTime.Parse("2000-03-01", CultureInfo.InvariantCulture));

            entity.Should().NotBeNull();
            entity!.Name.Should().Be("Sergio");
            entity!.Age.Should().Be(16);
        }

        [Fact]
        public async Task FindAsync_ExistingIdWithPastSnapshot_OnlyRelevantEventsApplied()
        {
            var eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12 }).Entity;
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

            var repo = new PersonDbEventStoreRepository(new EfCoreDbContext<TestDbContext>(_dbContext));

            var entity = await repo.FindAsync(
                p => p.Id == 12,
                DateTime.Parse("2000-03-01", CultureInfo.InvariantCulture));

            entity.Should().NotBeNull();
            entity!.Name.Should().Be("Sergio");
            entity!.Age.Should().Be(16);
        }

        [Fact]
        public void Remove_ExistingEntity_EntityDeleted()
        {
            var eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12 }).Entity;
            eventStore.Events.Add(
                new PersonEvent
                {
                    SerializedEvent = "{\"NewName\": \"Silverback\"}",
                    ClrType =
                        "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests"
                });
            _dbContext.SaveChanges();

            var repo = new PersonDbEventStoreRepository(new EfCoreDbContext<TestDbContext>(_dbContext));

            var entity = repo.Find(p => p.Id == 12);
            entity.Should().NotBeNull();

            repo.Remove(entity!);
            _dbContext.SaveChanges();

            _dbContext.Persons.Count().Should().Be(0);
            _dbContext.Persons.SelectMany(s => s.Events).Count().Should().Be(0);
        }

        [Fact]
        public async Task RemoveAsync_ExistingEntity_EntityDeleted()
        {
            var eventStore = _dbContext.Persons.Add(new PersonEventStore { Id = 12 }).Entity;
            eventStore.Events.Add(
                new PersonEvent
                {
                    SerializedEvent = "{\"NewName\": \"Silverback\"}",
                    ClrType =
                        "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests"
                });
            _dbContext.SaveChanges();

            var repo = new PersonDbEventStoreRepository(new EfCoreDbContext<TestDbContext>(_dbContext));

            var entity = await repo.FindAsync(p => p.Id == 12);
            entity.Should().NotBeNull();

            await repo.RemoveAsync(entity!);
            await _dbContext.SaveChangesAsync();

            _dbContext.Persons.Count().Should().Be(0);
            _dbContext.Persons.SelectMany(s => s.Events).Count().Should().Be(0);
        }

        [Fact]
        public void Remove_NonExistingEntity_ExceptionThrown()
        {
            var repo = new PersonDbEventStoreRepository(new EfCoreDbContext<TestDbContext>(_dbContext));

            var entity = new Person(123);

            Action act = () => repo.Remove(entity);

            act.Should().Throw<EventStoreNotFoundException>();
        }

        [Fact]
        public void RemoveAsync_NonExistingEntity_ExceptionThrown()
        {
            var repo = new PersonDbEventStoreRepository(new EfCoreDbContext<TestDbContext>(_dbContext));

            var entity = new Person(123);

            Func<Task> act = () => repo.RemoveAsync(entity);

            act.Should().Throw<EventStoreNotFoundException>();
        }

        public async ValueTask DisposeAsync()
        {
            if (_connection == null)
                return;

            _connection.Close();
            await _connection.DisposeAsync();

            await _dbContext.DisposeAsync();
        }
    }
}
