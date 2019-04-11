// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.EventStore;

namespace Silverback.Tests.EventSourcing.TestTypes
{
    public class PersonEventStore : EventStoreEntity<PersonEventStore.PersonEvent>
    {
        public int PersonId { get; set; }

        public string Ssn { get; set; }

        public class PersonEvent : EventEntity
        {
        }
    }
}