// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.EventStore;
using Silverback.Tests.EventSourcing.TestTypes;
using Xunit;

namespace Silverback.Tests.EventSourcing.EventStore
{
    public class EventStoreRepositoryTests
    {
        [Fact]
        public void Store_EntityWithSomeEvents_EventsSaved()
        {
            var repo = new PersonInMemoryEventStoreRepository();
            var person = new Person();
            person.ChangeName("Sergio");
            person.ChangeAge(35);

            repo.Store(person);

            repo.EventStores.Should().HaveCount(1);
            repo.EventStores.First().Events.Should().HaveCount(2);
        }

        [Fact]
        public async Task StoreAsync_EntityWithSomeEvents_EventsSaved()
        {
            var repo = new PersonInMemoryEventStoreRepository();
            var person = new Person();
            person.ChangeName("Sergio");
            person.ChangeAge(35);

            await repo.StoreAsync(person);

            repo.EventStores.Should().HaveCount(1);
            repo.EventStores.First().Events.Should().HaveCount(2);
        }

        [Fact]
        public void Store_ExistingEntity_NewEventsSaved()
        {
            var eventStore = new PersonEventStore { Id = 12, EntityVersion = 1 };
            eventStore.Events.Add(
                new PersonEvent
                {
                    SerializedEvent = "{\"NewName\": \"Silverback\"}",
                    ClrType =
                        "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests"
                });

            var repo = new PersonInMemoryEventStoreRepository(eventStore);

            var person = repo.GetById(12);

            person.ChangeName("Sergio");
            person.ChangeAge(35);

            repo.Store(person);

            repo.EventStores.Should().HaveCount(1);
            repo.EventStores.First().Events.Should().HaveCount(3);
        }

        [Fact]
        public async Task StoreAsync_ExistingEntity_NewEventsSaved()
        {
            var eventStore = new PersonEventStore { Id = 12, EntityVersion = 1 };
            eventStore.Events.Add(
                new PersonEvent
                {
                    SerializedEvent = "{\"NewName\": \"Silverback\"}",
                    ClrType =
                        "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests"
                });

            var repo = new PersonInMemoryEventStoreRepository(eventStore);

            var person = repo.GetById(12);

            person.ChangeName("Sergio");
            person.ChangeAge(35);

            await repo.StoreAsync(person);

            repo.EventStores.Should().HaveCount(1);
            repo.EventStores.First().Events.Should().HaveCount(3);
        }

        [Fact]
        public void Store_EntityWithSomeEvents_VersionCalculated()
        {
            var repo = new PersonInMemoryEventStoreRepository();
            var person = new Person();
            person.ChangeName("Sergio");
            person.ChangeAge(35);

            repo.Store(person);

            repo.EventStores.First().EntityVersion.Should().Be(2);
        }

        [Fact]
        public async Task StoreAsync_EntityWithSomeEvents_VersionCalculated()
        {
            var repo = new PersonInMemoryEventStoreRepository();
            var person = new Person();
            person.ChangeName("Sergio");
            person.ChangeAge(35);

            await repo.StoreAsync(person);

            repo.EventStores.First().EntityVersion.Should().Be(2);
        }

        [Fact]
        public void Store_ExistingEntity_VersionIncremented()
        {
            var eventStore = new PersonEventStore { Id = 12, EntityVersion = 1 };
            eventStore.Events.Add(
                new PersonEvent
                {
                    SerializedEvent = "{\"NewName\": \"Silverback\"}",
                    ClrType =
                        "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests"
                });

            var repo = new PersonInMemoryEventStoreRepository(eventStore);

            var person = repo.GetById(12);

            person.ChangeName("Sergio");
            person.ChangeAge(35);

            repo.Store(person);

            repo.EventStores.First().EntityVersion.Should().Be(3);
        }

        [Fact]
        public async Task StoreAsync_ExistingEntity_VersionIncremented()
        {
            var eventStore = new PersonEventStore { Id = 12, EntityVersion = 1 };
            eventStore.Events.Add(
                new PersonEvent
                {
                    SerializedEvent = "{\"NewName\": \"Silverback\"}",
                    ClrType =
                        "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests"
                });

            var repo = new PersonInMemoryEventStoreRepository(eventStore);

            var person = repo.GetById(12);

            person.ChangeName("Sergio");
            person.ChangeAge(35);

            await repo.StoreAsync(person);

            repo.EventStores.First().EntityVersion.Should().Be(3);
        }

        [Fact]
        public void Store_ConcurrentlyModifyExistingEntity_ExceptionThrown()
        {
            var eventStore = new PersonEventStore { Id = 12, EntityVersion = 1 };
            eventStore.Events.Add(
                new PersonEvent
                {
                    SerializedEvent = "{\"NewName\": \"Silverback\"}",
                    ClrType =
                        "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests"
                });

            var repo = new PersonInMemoryEventStoreRepository(eventStore);

            var person = repo.GetById(12);
            var person2 = repo.GetById(12);

            person.ChangeName("Sergio");
            person.ChangeAge(35);
            person2.ChangeName("Sergio");
            person2.ChangeAge(35);

            repo.Store(person);
            Action act = () => repo.Store(person2);

            act.Should().Throw<EventStoreConcurrencyException>();
        }

        [Fact]
        public async Task StoreAsync_ConcurrentlyModifyExistingEntity_ExceptionThrown()
        {
            var eventStore = new PersonEventStore { Id = 12, EntityVersion = 1 };
            eventStore.Events.Add(
                new PersonEvent
                {
                    SerializedEvent = "{\"NewName\": \"Silverback\"}",
                    ClrType =
                        "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests"
                });

            var repo = new PersonInMemoryEventStoreRepository(eventStore);

            var person = repo.GetById(12);
            var person2 = repo.GetById(12);

            person.ChangeName("Sergio");
            person.ChangeAge(35);
            person2.ChangeName("Sergio");
            person2.ChangeAge(35);

            await repo.StoreAsync(person);
            Func<Task> act = async () => await repo.StoreAsync(person2);

            act.Should().Throw<EventStoreConcurrencyException>();
        }

        [Fact]
        public void GetAggregateEntity_ExistingId_EntityRecreated()
        {
            var eventStore = new PersonEventStore { Id = 12 };
            eventStore.Events.Add(
                new PersonEvent
                {
                    SerializedEvent = "{\"NewName\": \"Silverback\"}",
                    ClrType =
                        "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests"
                });

            var repo = new PersonInMemoryEventStoreRepository(eventStore);

            var entity = repo.GetById(12);

            entity.Should().NotBe(null);
        }

        [Fact]
        public void GetAggregateEntity_ExistingId_EventsApplied()
        {
            var eventStore = new PersonEventStore { Id = 12 };
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

            var repo = new PersonInMemoryEventStoreRepository(eventStore);

            var entity = repo.GetById(12);

            entity.Name.Should().Be("Silverback");
            entity.Age.Should().Be(35);
        }

        [Fact]
        public void GetAggregateEntity_ExistingId_EventsAppliedInRightOrder()
        {
            var eventStore = new PersonEventStore { Id = 12 };
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
            var repo = new PersonInMemoryEventStoreRepository(eventStore);

            var entity = repo.GetById(12);

            entity.Name.Should().Be("Silverback");
        }

        [Fact]
        public void GetAggregateEntity_EventsWithLegacySerialization_EventsApplied()
        {
            var eventStore = new PersonEventStore { Id = 12 };
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
            var repo = new PersonInMemoryEventStoreRepository(eventStore);

            var entity = repo.GetById(12);

            entity.Name.Should().Be("Silverback");
            entity.Age.Should().Be(35);
        }

        [Fact]
        public void GetAggregateEntity_ExistingIdWithPastSnapshot_OnlyRelevantEventsApplied()
        {
            var eventStore = new PersonEventStore { Id = 12 };
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

            var repo = new PersonInMemoryEventStoreRepository(eventStore);

            var entity = repo.GetSnapshotById(12, DateTime.Parse("2000-03-01", CultureInfo.InvariantCulture));

            entity.Name.Should().Be("Sergio");
            entity.Age.Should().Be(16);
        }

        [Fact]
        public void Remove_ExistingEntity_EntityDeleted()
        {
            var eventStore = new PersonEventStore { Id = 12 };
            eventStore.Events.Add(
                new PersonEvent
                {
                    SerializedEvent = "{\"NewName\": \"Silverback\"}",
                    ClrType =
                        "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests"
                });

            var repo = new PersonInMemoryEventStoreRepository(eventStore);

            var entity = repo.GetById(12);
            entity.Should().NotBeNull();

            repo.Remove(entity);

            repo.EventStores.Should().BeEmpty();
            repo.EventStores.SelectMany(s => s.Events).Should().BeEmpty();
        }

        [Fact]
        public async Task RemoveAsync_ExistingEntity_EntityDeleted()
        {
            var eventStore = new PersonEventStore { Id = 12 };
            eventStore.Events.Add(
                new PersonEvent
                {
                    SerializedEvent = "{\"NewName\": \"Silverback\"}",
                    ClrType =
                        "Silverback.Tests.EventSourcing.TestTypes.EntityEvents.NameChangedEvent, Silverback.EventSourcing.Tests"
                });

            var repo = new PersonInMemoryEventStoreRepository(eventStore);

            var entity = repo.GetById(12);
            entity.Should().NotBeNull();

            await repo.RemoveAsync(entity);

            repo.EventStores.Should().BeEmpty();
            repo.EventStores.SelectMany(s => s.Events).Should().BeEmpty();
        }

        [Fact]
        public void Remove_NonExistingEntity_ExceptionThrown()
        {
            var repo = new PersonInMemoryEventStoreRepository();

            var entity = new Person(123);

            Action act = () => repo.Remove(entity);

            act.Should().Throw<EventStoreNotFoundException>();
        }

        [Fact]
        public void RemoveAsync_NonExistingEntity_ExceptionThrown()
        {
            var repo = new PersonInMemoryEventStoreRepository();

            var entity = new Person(123);

            Func<Task> act = () => repo.RemoveAsync(entity);

            act.Should().Throw<EventStoreNotFoundException>();
        }
    }
}
