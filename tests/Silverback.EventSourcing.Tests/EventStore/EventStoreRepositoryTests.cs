// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Tests.EventSourcing.TestTypes;
using Xunit;

namespace Silverback.Tests.EventSourcing.EventStore
{
    public class EventStoreRepositoryTests
    {
        #region Store (Basics)

        [Fact]
        public void Store_EntityWithSomeEvents_EventsSaved()
        {
            var repo = new PersonInMemoryEventStoreRepository();
            var person = new Person();
            person.ChangeName("Sergio");
            person.ChangeAge(35);

            repo.Store(person);

            repo.EventStores.Count.Should().Be(1);
            repo.EventStores.First().Events.Count.Should().Be(2);
        }

        [Fact]
        public async Task StoreAsync_EntityWithSomeEvents_EventsSaved()
        {
            var repo = new PersonInMemoryEventStoreRepository();
            var person = new Person();
            person.ChangeName("Sergio");
            person.ChangeAge(35);

            await repo.StoreAsync(person);

            repo.EventStores.Count.Should().Be(1);
            repo.EventStores.First().Events.Count.Should().Be(2);
        }

        [Fact]
        public void Store_ExistingEntity_NewEventsSaved()
        {
            var eventStore = new PersonEventStore { Id = 12, EntityVersion = 1 };
            eventStore.Events.Add(new PersonEvent
            {
                SerializedEvent = "{" +
                                  "\"$type\": \"Silverback.Tests.EventSourcing.TestTypes.Person+NameChangedEvent, Silverback.EventSourcing.Tests\"," +
                                  "\"NewName\": \"Silverback\"" +
                                  "}"
            });

            var repo = new PersonInMemoryEventStoreRepository(eventStore);

            var person = repo.GetById(12);

            person.ChangeName("Sergio");
            person.ChangeAge(35);

            repo.Store(person);

            repo.EventStores.Count.Should().Be(1);
            repo.EventStores.First().Events.Count.Should().Be(3);
        }

        [Fact]
        public async Task StoreAsync_ExistingEntity_NewEventsSaved()
        {
            var eventStore = new PersonEventStore { Id = 12, EntityVersion = 1 };
            eventStore.Events.Add(new PersonEvent
            {
                SerializedEvent = "{" +
                                  "\"$type\": \"Silverback.Tests.EventSourcing.TestTypes.Person+NameChangedEvent, Silverback.EventSourcing.Tests\"," +
                                  "\"NewName\": \"Silverback\"" +
                                  "}"
            });

            var repo = new PersonInMemoryEventStoreRepository(eventStore);

            var person = repo.GetById(12);

            person.ChangeName("Sergio");
            person.ChangeAge(35);

            await repo.StoreAsync(person);

            repo.EventStores.Count.Should().Be(1);
            repo.EventStores.First().Events.Count.Should().Be(3);
        }

        #endregion

        #region Store (Concurrency)

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
            eventStore.Events.Add(new PersonEvent
            {
                SerializedEvent = "{" +
                                  "\"$type\": \"Silverback.Tests.EventSourcing.TestTypes.Person+NameChangedEvent, Silverback.EventSourcing.Tests\"," +
                                  "\"NewName\": \"Silverback\"" +
                                  "}"
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
            eventStore.Events.Add(new PersonEvent
            {
                SerializedEvent = "{" +
                                  "\"$type\": \"Silverback.Tests.EventSourcing.TestTypes.Person+NameChangedEvent, Silverback.EventSourcing.Tests\"," +
                                  "\"NewName\": \"Silverback\"" +
                                  "}"
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
            eventStore.Events.Add(new PersonEvent
            {
                SerializedEvent = "{" +
                                  "\"$type\": \"Silverback.Tests.EventSourcing.TestTypes.Person+NameChangedEvent, Silverback.EventSourcing.Tests\"," +
                                  "\"NewName\": \"Silverback\"" +
                                  "}"
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

            act.Should().Throw<SilverbackConcurrencyException>();
        }

        [Fact]
        public async Task StoreAsync_ConcurrentlyModifyExistingEntity_ExceptionThrown()
        {
            var eventStore = new PersonEventStore { Id = 12, EntityVersion = 1 };
            eventStore.Events.Add(new PersonEvent
            {
                SerializedEvent = "{" +
                                  "\"$type\": \"Silverback.Tests.EventSourcing.TestTypes.Person+NameChangedEvent, Silverback.EventSourcing.Tests\"," +
                                  "\"NewName\": \"Silverback\"" +
                                  "}"
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

            act.Should().Throw<SilverbackConcurrencyException>();
        }

        #endregion

        #region GetAggregateEntity

        [Fact]
        public void GetAggregateEntity_ExistingId_EntityRecreated()
        {
            var eventStore = new PersonEventStore { Id = 12 };
            eventStore.Events.Add(new PersonEvent
            {
                SerializedEvent = "{" +
                                  "\"$type\": \"Silverback.Tests.EventSourcing.TestTypes.Person+NameChangedEvent, Silverback.EventSourcing.Tests\"," +
                                  "\"NewName\": \"Silverback\"" +
                                  "}"
            });

            var repo = new PersonInMemoryEventStoreRepository(eventStore);

            var entity = repo.GetById(12);

            entity.Should().NotBe(null);
        }

        [Fact]
        public void GetAggregateEntity_ExistingId_EventsApplied()
        {
            var eventStore = new PersonEventStore { Id = 12 };
            eventStore.Events.Add(new PersonEvent
            {
                SerializedEvent = "{" +
                                  "\"$type\": \"Silverback.Tests.EventSourcing.TestTypes.Person+NameChangedEvent, Silverback.EventSourcing.Tests\"," +
                                  "\"NewName\": \"Silverback\"" +
                                  "}"
            });
            eventStore.Events.Add(new PersonEvent
            {
                SerializedEvent = "{" +
                                  "\"$type\": \"Silverback.Tests.EventSourcing.TestTypes.Person+AgeChangedEvent, Silverback.EventSourcing.Tests\"," +
                                  "\"NewAge\": 35" +
                                  "}"
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
            eventStore.Events.Add(new PersonEvent
            {
                SerializedEvent = "{" +
                                  "\"$type\": \"Silverback.Tests.EventSourcing.TestTypes.Person+NameChangedEvent, Silverback.EventSourcing.Tests\"," +
                                  "\"NewName\": \"Silverback\"" +
                                  "}",
                Timestamp = DateTime.Parse("2000-05-05")
            });
            eventStore.Events.Add(new PersonEvent
            {
                SerializedEvent = "{" +
                                  "\"$type\": \"Silverback.Tests.EventSourcing.TestTypes.Person+NameChangedEvent, Silverback.EventSourcing.Tests\"," +
                                  "\"NewName\": \"Sergio\"" +
                                  "}",
                Timestamp = DateTime.Parse("2000-03-01")
            });
            var repo = new PersonInMemoryEventStoreRepository(eventStore);

            var entity = repo.GetById(12);

            entity.Name.Should().Be("Silverback");
        }

        [Fact]
        public void GetAggregateEntity_NonExistingId_NullReturned()
        {
            var repo = new PersonInMemoryEventStoreRepository();

            var entity = repo.GetById(11);

            entity.Should().BeNull();
        }

        [Fact]
        public void GetAggregateEntity_ExistingIdWithPastSnapshot_OnlyRelevantEventsApplied()
        {
            var eventStore = new PersonEventStore { Id = 12 };
            eventStore.Events.Add(new PersonEvent
            {
                SerializedEvent = "{" +
                                  "\"$type\": \"Silverback.Tests.EventSourcing.TestTypes.Person+NameChangedEvent, Silverback.EventSourcing.Tests\"," +
                                  "\"NewName\": \"Silverback\"" +
                                  "}",
                Timestamp = DateTime.Parse("2000-05-05")
            });
            eventStore.Events.Add(new PersonEvent
            {
                SerializedEvent = "{" +
                                  "\"$type\": \"Silverback.Tests.EventSourcing.TestTypes.Person+NameChangedEvent, Silverback.EventSourcing.Tests\"," +
                                  "\"NewName\": \"Sergio\"" +
                                  "}",
                Timestamp = DateTime.Parse("2000-03-01")
            });
            eventStore.Events.Add(new PersonEvent
            {
                SerializedEvent = "{" +
                                  "\"$type\": \"Silverback.Tests.EventSourcing.TestTypes.Person+AgeChangedEvent, Silverback.EventSourcing.Tests\"," +
                                  "\"NewAge\": 16" +
                                  "}",
                Timestamp = DateTime.Parse("2000-02-01")
            });
            eventStore.Events.Add(new PersonEvent
            {
                SerializedEvent = "{" +
                                  "\"$type\": \"Silverback.Tests.EventSourcing.TestTypes.Person+AgeChangedEvent, Silverback.EventSourcing.Tests\"," +
                                  "\"NewAge\": 35" +
                                  "}",
                Timestamp = DateTime.Parse("2019-07-06")
            });

            var repo = new PersonInMemoryEventStoreRepository(eventStore);

            var entity = repo.GetSnapshotById(12, DateTime.Parse("2000-03-01"));

            entity.Name.Should().Be("Sergio");
            entity.Age.Should().Be(16);
        }

        #endregion

        #region Remove

        [Fact]
        public void Remove_ExistingEntity_EntityDeleted()
        {
            var eventStore = new PersonEventStore { Id = 12 };
            eventStore.Events.Add(new PersonEvent
            {
                SerializedEvent = "{" +
                                  "\"$type\": \"Silverback.Tests.EventSourcing.TestTypes.Person+NameChangedEvent, Silverback.EventSourcing.Tests\"," +
                                  "\"NewName\": \"Silverback\"" +
                                  "}"
            });

            var repo = new PersonInMemoryEventStoreRepository(eventStore);

            var entity = repo.GetById(12);
            entity.Should().NotBeNull();

            repo.Remove(entity);

            repo.EventStores.Count.Should().Be(0);
            repo.EventStores.SelectMany(s => s.Events).Count().Should().Be(0);
        }

        [Fact]
        public async Task RemoveAsync_ExistingEntity_EntityDeleted()
        {
            var eventStore = new PersonEventStore { Id = 12 };
            eventStore.Events.Add(new PersonEvent
            {
                SerializedEvent = "{" +
                                  "\"$type\": \"Silverback.Tests.EventSourcing.TestTypes.Person+NameChangedEvent, Silverback.EventSourcing.Tests\"," +
                                  "\"NewName\": \"Silverback\"" +
                                  "}"
            });

            var repo = new PersonInMemoryEventStoreRepository(eventStore);

            var entity = repo.GetById(12);
            entity.Should().NotBeNull();

            await repo.RemoveAsync(entity);

            repo.EventStores.Count.Should().Be(0);
            repo.EventStores.SelectMany(s => s.Events).Count().Should().Be(0);
        }

        [Fact]
        public void Remove_NonExistingEntity_ReturnsNull()
        {
            var repo = new PersonInMemoryEventStoreRepository();

            var entity = new Person(123);

            var result = repo.Remove(entity);

            result.Should().BeNull();
        }

        [Fact]
        public async Task RemoveAsync_NonExistingEntity_ReturnsNull()
        {
            var repo = new PersonInMemoryEventStoreRepository();

            var entity = new Person(123);

            var result = await repo.RemoveAsync(entity);

            result.Should().BeNull();
        }

        #endregion
    }
}