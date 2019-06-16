// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Core.EntityFrameworkCore.TestTypes.Base.Domain
{
    public abstract class DomainEntity : IMessagesSource
    {
        private List<IDomainEvent> _domainEvents;

        #region IMessagesSource

        public IEnumerable<object> GetMessages() => _domainEvents;

        public void ClearMessages() => _domainEvents.Clear();

        #endregion

        protected void AddEvent(IDomainEvent domainEvent)
        {
            _domainEvents = _domainEvents ?? new List<IDomainEvent>();

            domainEvent.Source = this;

            _domainEvents.Add(domainEvent);
        }

        protected TEvent AddEvent<TEvent>()
            where TEvent : IDomainEvent, new()
        {
            var @event = new TEvent();
            AddEvent(@event);
            return @event;
        }

        protected void RemoveEvent(IDomainEvent<DomainEntity> domainEvent) => _domainEvents?.Remove(domainEvent);
    }
}
