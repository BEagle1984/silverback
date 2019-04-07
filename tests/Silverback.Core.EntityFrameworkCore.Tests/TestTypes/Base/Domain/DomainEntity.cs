// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Silverback.Tests.Core.EntityFrameworkCore.TestTypes.Base.Domain
{
    /// <summary>
    /// A sample implementation of <see cref="IDomainEntity"/>.
    /// </summary>
    public abstract class DomainEntity : IDomainEntity
    {
        private List<IDomainEvent<IDomainEntity>> _domainEvents;

        [NotMapped]
        public IEnumerable<IDomainEvent<IDomainEntity>> DomainEvents =>
            _domainEvents?.AsReadOnly() ?? Enumerable.Empty<IDomainEvent<IDomainEntity>>();

        public void ClearEvents() => _domainEvents?.Clear();

        protected void AddEvent(IDomainEvent<IDomainEntity> domainEvent)
        {
            _domainEvents = _domainEvents ?? new List<IDomainEvent<IDomainEntity>>();

            ((IDomainEvent)domainEvent).Source = this;

            _domainEvents.Add(domainEvent);
        }

        protected TEvent AddEvent<TEvent>()
            where TEvent : IDomainEvent<IDomainEntity>, new()
        {
            var evnt = new TEvent();
            AddEvent(evnt);
            return evnt;
        }

        protected void RemoveEvent(IDomainEvent<DomainEntity> domainEvent) => _domainEvents?.Remove(domainEvent);
    }
}
