// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Silverback.Tests.EventSourcing.TestTypes
{
    public class PersonEventStoreRepository : InMemoryEventStoreRepository<Person, PersonEventStore, PersonEventStore.PersonEvent>
    {
        public PersonEventStoreRepository()
        {
        }

        public PersonEventStoreRepository(params PersonEventStore[] eventStoreEntities)
            : this(eventStoreEntities.AsEnumerable())
        {
        }

        public PersonEventStoreRepository(IEnumerable<PersonEventStore> eventStoreEntities)
        {
            EventStores.AddRange(eventStoreEntities);
        }

        public Person GetById(int id) => GetAggregateEntity(EventStores.FirstOrDefault(x => x.PersonId == id));

        public Person GetBySsn(string ssn) => GetAggregateEntity(EventStores.FirstOrDefault(x => x.Ssn == ssn));

        public Person GetSnapshotById(int id, DateTime snapshot) => GetAggregateEntity(EventStores.FirstOrDefault(x => x.PersonId == id), snapshot);

        protected override PersonEventStore GetEventStoreEntity(Person aggregateEntity, bool addIfNotFound)
        {
            var store = EventStores.FirstOrDefault(s => s.PersonId == aggregateEntity.Id);

            if (store == null && addIfNotFound)
            {
                store = new PersonEventStore {PersonId = aggregateEntity.Id, Ssn = aggregateEntity.Ssn};
                EventStores.Add(store);
            }

            return store;
        }

        protected override Task<PersonEventStore> GetEventStoreEntityAsync(Person aggregateEntity, bool addIfNotFound) =>
            Task.FromResult(GetEventStoreEntity(aggregateEntity, addIfNotFound));
    }
}