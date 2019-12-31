// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Silverback.Domain.Util;
using Silverback.EventStore;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Domain
{
    public abstract class EventSourcingDomainEntity<TKey> : EventSourcingDomainEntity<TKey, object>
    {
        protected EventSourcingDomainEntity()
        {
        }

        protected EventSourcingDomainEntity(IEnumerable<IEntityEvent> events)
            : base(events)
        {
        }
    }

    public abstract class EventSourcingDomainEntity<TKey, TDomainEvent>
        : MessagesSource<TDomainEvent>,
            IEventSourcingAggregate<TKey>
    {
        private readonly List<IEntityEvent> _storedEvents;
        private List<IEntityEvent> _newEvents;

        protected EventSourcingDomainEntity()
        {
        }

        protected EventSourcingDomainEntity(IEnumerable<IEntityEvent> events)
        {
            events = events.OrderBy(e => e.Timestamp).ThenBy(e => e.Sequence).ToList().AsReadOnly();

            events.ForEach(e => EventsApplier.Apply(e, this, true));

            _storedEvents = events.ToList();

            ClearMessages();
        }

        [NotMapped]
        public IEnumerable<TDomainEvent> DomainEvents =>
            GetMessages()?.Cast<TDomainEvent>() ?? Enumerable.Empty<TDomainEvent>();

        [NotMapped]
        public IEnumerable<IEntityEvent> Events =>
            (_storedEvents ?? Enumerable.Empty<IEntityEvent>()).Union(_newEvents ?? Enumerable.Empty<IEntityEvent>())
            .ToList().AsReadOnly();

        public TKey Id { get; protected set; }

        protected virtual IEntityEvent AddAndApplyEvent(IEntityEvent @event)
        {
            EventsApplier.Apply(@event, this);

            _newEvents ??= new List<IEntityEvent>();
            _newEvents.Add(@event);

            if (@event.Timestamp == default)
                @event.Timestamp = DateTime.UtcNow;

            if (@event.Sequence <= 0)
                @event.Sequence = (_storedEvents?.Count ?? 0) + _newEvents.Count;

            return @event;
        }

        public int GetVersion() => Events.Count();

        public IEnumerable<IEntityEvent> GetNewEvents() => _newEvents?.AsReadOnly() ?? Enumerable.Empty<IEntityEvent>();
    }
}