// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Domain;

namespace Silverback.Tests.Core.Model.TestTypes.Domain
{
    public class TestAggregateRoot : DomainEntity, IAggregateRoot
    {
        public new void AddEvent(IDomainEvent domainEvent)
            => base.AddEvent(domainEvent);

        public new TEvent AddEvent<TEvent>(bool allowMultiple = true)
            where TEvent : IDomainEvent, new()
            => base.AddEvent<TEvent>(allowMultiple);
    }
}