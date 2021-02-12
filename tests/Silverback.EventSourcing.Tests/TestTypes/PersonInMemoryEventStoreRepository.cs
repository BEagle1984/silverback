// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Util;

namespace Silverback.Tests.EventSourcing.TestTypes
{
    public class PersonInMemoryEventStoreRepository
        : InMemoryEventStoreRepository<Person, PersonEventStore, PersonEvent>
    {
        public PersonInMemoryEventStoreRepository()
        {
        }

        public PersonInMemoryEventStoreRepository(params PersonEventStore[] eventStoreEntities)
            : this(eventStoreEntities.AsEnumerable())
        {
        }

        public PersonInMemoryEventStoreRepository(IEnumerable<PersonEventStore> eventStoreEntities)
        {
            eventStoreEntities.ForEach(personEventStore => EventStores.Add(personEventStore));
        }

        public Person GetById(int id) => GetDomainEntity(EventStores.First(x => x.Id == id));

        public Person GetBySsn(string ssn) => GetDomainEntity(EventStores.First(x => x.Ssn == ssn));

        public Person GetSnapshotById(int id, DateTime snapshot) =>
            GetDomainEntity(EventStores.First(x => x.Id == id), snapshot);

        protected override void AddEventStoreEntity(PersonEventStore eventStoreEntity) =>
            EventStores.Add(eventStoreEntity);

        protected override PersonEventStore? GetEventStoreEntity(Person domainEntity) =>
            EventStores.FirstOrDefault(s => s.Id == domainEntity.Id);

        protected override Task<PersonEventStore?> GetEventStoreEntityAsync(Person domainEntity) =>
            Task.FromResult(GetEventStoreEntity(domainEntity));
    }
}
