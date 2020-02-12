// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.ComponentModel.DataAnnotations;
using Silverback.EventStore;

namespace Silverback.Tests.EventSourcing.TestTypes
{
    public class PersonEventStore : EventStoreEntity<PersonEvent>
    {
        [Key]
        public int Id { get; set; }

        public string Ssn { get; set; }
    }
}