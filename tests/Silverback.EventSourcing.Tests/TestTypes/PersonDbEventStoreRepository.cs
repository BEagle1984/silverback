// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Database;
using Silverback.EventStore;

namespace Silverback.Tests.EventSourcing.TestTypes
{
    public class PersonDbEventStoreRepository : DbEventStoreRepository<Person, int, PersonEventStore, PersonEvent>
    {
        public PersonDbEventStoreRepository(IDbContext dbContext)
            : base(dbContext)
        {
        }
    }
}