// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.EntityFrameworkCore;
using Silverback.EventStore;

namespace Silverback.Tests.EventSourcing.EntityFrameworkCore.TestTypes
{
    public class PersonEventStoreRepository : DbContextEventStoreRepository<Person, int, PersonEventStore, PersonEvent>
    {
        public PersonEventStoreRepository(DbContext dbContext) : base(dbContext)
        {
        }

        protected override PersonEventStore GetNewEventStoreEntity(Person aggregateEntity) =>
            new PersonEventStore
            {
                Id = aggregateEntity.Id,
                Ssn = aggregateEntity.Ssn
            };
    }
}
