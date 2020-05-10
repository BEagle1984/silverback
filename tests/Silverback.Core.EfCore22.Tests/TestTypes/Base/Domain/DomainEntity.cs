// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Core.EFCore22.TestTypes.Base.Domain
{
    public abstract class DomainEntity : IMessagesSource
    {
        private List<IDomainEvent>? _domainEvents;

        public IEnumerable<object> GetMessages() => _domainEvents ?? Enumerable.Empty<object>();

        public void ClearMessages() => _domainEvents?.Clear();

        protected void AddEvent(IDomainEvent domainEvent)
        {
            _domainEvents ??= new List<IDomainEvent>();

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