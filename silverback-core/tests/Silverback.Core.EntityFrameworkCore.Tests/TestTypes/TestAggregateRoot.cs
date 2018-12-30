// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.ComponentModel.DataAnnotations;
using Silverback.Domain;

namespace Silverback.Core.EntityFrameworkCore.Tests.TestTypes
{
    public class TestAggregateRoot : DomainEntity, IAggregateRoot
    {
        [Key]
        public int Id { get; set; }

        public new void AddEvent(IDomainEvent<IDomainEntity> domainEvent)
            => base.AddEvent(domainEvent);

        public new TEvent AddEvent<TEvent>() 
            where TEvent : IDomainEvent<IDomainEntity>, new()
            => base.AddEvent<TEvent>();
    }
}
