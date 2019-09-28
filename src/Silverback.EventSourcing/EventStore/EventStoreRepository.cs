// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Domain;
using Silverback.Domain.Util;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.EventStore
{
    public abstract class EventStoreRepository<TAggregateEntity, TEventStoreEntity, TEventEntity>
        where TAggregateEntity : IEventSourcingAggregate
        where TEventStoreEntity : IEventStoreEntity<TEventEntity>
        where TEventEntity : IEventEntity, new()
    {
        public TEventStoreEntity Store(TAggregateEntity aggregateEntity) =>
            Store(aggregateEntity, GetEventStoreEntity(aggregateEntity, true));

        public async Task<TEventStoreEntity> StoreAsync(TAggregateEntity aggregateEntity) =>
            Store(aggregateEntity, await GetEventStoreEntityAsync(aggregateEntity, true));

        public TEventStoreEntity Remove(TAggregateEntity aggregateEntity) =>
            Remove(aggregateEntity, GetEventStoreEntity(aggregateEntity, false));

        public async Task<TEventStoreEntity> RemoveAsync(TAggregateEntity aggregateEntity) =>
            Remove(aggregateEntity, await GetEventStoreEntityAsync(aggregateEntity, false));

        protected abstract TEventStoreEntity GetEventStoreEntity(TAggregateEntity aggregateEntity, bool addIfNotFound);

        protected abstract Task<TEventStoreEntity> GetEventStoreEntityAsync(TAggregateEntity aggregateEntity, bool addIfNotFound);

        protected virtual TAggregateEntity GetAggregateEntity(TEventStoreEntity eventStore, DateTime? snapshot = null)
        {
            if (eventStore == null)
                return default;

            var events = snapshot != null 
                ? eventStore.Events.Where(e => e.Timestamp <= snapshot) 
                : eventStore.Events;

            return EntityActivator.CreateInstance<TAggregateEntity>(
                events
                    .OrderBy(e => e.Timestamp).ThenBy(e => e.Sequence)
                    .Select(GetEvent),
                eventStore);
        }

        protected virtual TEventEntity GetEventEntity(IEntityEvent @event) =>
            new TEventEntity
            {
                SerializedEvent = EventSerializer.Serialize(@event),
                Timestamp = @event.Timestamp,
                Sequence = @event.Sequence
            };

        protected virtual IEntityEvent GetEvent(TEventEntity e)
        {
            var @event = EventSerializer.Deserialize(e.SerializedEvent);
            @event.Sequence = e.Sequence;
            @event.Timestamp = e.Timestamp;
            return @event;
        }

        protected abstract TEventStoreEntity Remove(TAggregateEntity aggregateEntity, TEventStoreEntity eventStore);

        private TEventStoreEntity Store(TAggregateEntity aggregateEntity, TEventStoreEntity eventStore)
        {
            var newEvents = aggregateEntity.GetNewEvents();

            eventStore.EntityVersion += newEvents.Count();

            if (eventStore.EntityVersion != aggregateEntity.GetVersion())
                throw new SilverbackConcurrencyException(
                    $"Expected to save version {aggregateEntity.GetVersion()} but new version was {eventStore.EntityVersion}. " +
                    "Refresh the aggregate entity and reapply the new events.");

            newEvents.ForEach(@event => eventStore.Events.Add(
                GetEventEntity(@event)));

            // Move the domain events from the aggregate to the event store entity
            // being persisted, in order for Silverback to automatically publish them
            // (even if the aggregate itself is not stored).
            if (aggregateEntity is IMessagesSource messagesSource)
            {
                eventStore.AddDomainEvents(messagesSource.GetMessages());
                messagesSource.ClearMessages();
            }

            return eventStore;
        }
    }
}