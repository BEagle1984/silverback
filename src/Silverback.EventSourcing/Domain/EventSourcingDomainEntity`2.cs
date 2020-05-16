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
    /// <summary> The base class for the domain entities that are persisted in the event store. </summary>
    /// <remarks>
    ///     It's not mandatory to use this base class as long as long as the domain entities implement the
    ///     <see cref="IEventSourcingDomainEntity{TKey}" /> interface.
    /// </remarks>
    /// <typeparam name="TKey"> The type of the entity key. </typeparam>
    /// <typeparam name="TDomainEvent"> The base type of the domain events. </typeparam>
    public abstract class EventSourcingDomainEntity<TKey, TDomainEvent>
        : MessagesSource<TDomainEvent>, IEventSourcingDomainEntity<TKey>
    {
        private readonly List<IEntityEvent>? _storedEvents;

        private List<IEntityEvent>? _newEvents;

        /// <summary>
        ///     Initializes a new instance of the <see cref="EventSourcingDomainEntity{TKey, TDomainEvent}" />
        ///     class.
        /// </summary>
        protected EventSourcingDomainEntity()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="EventSourcingDomainEntity{TKey, TDomainEvent}" />
        ///     class from the stored events.
        /// </summary>
        /// <param name="events"> The stored events to be re-applied to rebuild the entity state. </param>
        protected EventSourcingDomainEntity(IReadOnlyCollection<IEntityEvent> events)
        {
            Check.NotEmpty(events, nameof(events));

            events = events.OrderBy(e => e.Timestamp).ThenBy(e => e.Sequence).ToList().AsReadOnly();

            events.ForEach(e => EventsApplier.Apply(e, this, true));

            _storedEvents = events.ToList();

            ClearMessages();
        }

        /// <summary> Gets the domain events that have been added but not yet published. </summary>
        [NotMapped]
        public IEnumerable<TDomainEvent> DomainEvents =>
            GetMessages()?.Cast<TDomainEvent>() ?? Enumerable.Empty<TDomainEvent>();

        /// <summary> Gets the events that have been applied to build the current state. </summary>
        [NotMapped]
        public IEnumerable<IEntityEvent> Events =>
            (_storedEvents ?? Enumerable.Empty<IEntityEvent>()).Union(_newEvents ?? Enumerable.Empty<IEntityEvent>())
            .ToList().AsReadOnly();

        /// <inheritdoc />
        public TKey Id { get; protected set; } = default!;

        /// <inheritdoc />
        public int GetVersion() => Events.Count();

        /// <inheritdoc />
        public IEnumerable<IEntityEvent> GetNewEvents() => _newEvents?.AsReadOnly() ?? Enumerable.Empty<IEntityEvent>();

        /// <summary> Adds the specified event and applies it to update the entity state. </summary>
        /// <param name="entityEvent"> The event to be added. </param>
        /// <returns> The <see cref="IEntityEvent" /> that was added and applied. </returns>
        protected virtual IEntityEvent AddAndApplyEvent(IEntityEvent entityEvent)
        {
            Check.NotNull(entityEvent, nameof(entityEvent));

            EventsApplier.Apply(entityEvent, this);

            _newEvents ??= new List<IEntityEvent>();
            _newEvents.Add(entityEvent);

            if (entityEvent.Timestamp == default)
                entityEvent.Timestamp = DateTime.UtcNow;

            if (entityEvent.Sequence <= 0)
                entityEvent.Sequence = (_storedEvents?.Count ?? 0) + _newEvents.Count;

            return entityEvent;
        }
    }
}
