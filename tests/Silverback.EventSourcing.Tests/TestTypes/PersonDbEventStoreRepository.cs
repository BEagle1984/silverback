// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.EntityFrameworkCore;
using Silverback.Database;
using Silverback.EventStore;

namespace Silverback.Tests.EventSourcing.TestTypes
{
    public class PersonDbEventStoreRepository : DbEventStoreRepository<Person, int, PersonEventStore, PersonEvent>
    {
        public PersonDbEventStoreRepository(IDbContext dbContext) : base(dbContext)
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
