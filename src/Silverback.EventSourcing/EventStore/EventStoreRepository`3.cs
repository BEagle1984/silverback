// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Silverback.Domain;
using Silverback.Domain.Util;
using Silverback.Messaging.Messages;
using Silverback.Serialization;
using Silverback.Util;

namespace Silverback.EventStore
{
    /// <summary>
    ///     The base class for the event store repositories.
    /// </summary>
    /// <typeparam name="TDomainEntity">
    ///     The type of the domain entity whose events are stored in this repository.
    /// </typeparam>
    /// <typeparam name="TEventStoreEntity">
    ///     The type of event store entity being persisted to the underlying storage.
    /// </typeparam>
    /// <typeparam name="TEventEntity">
    ///     The base type of the events that will be associated to the event store entity.
    /// </typeparam>
    public abstract class EventStoreRepository<TDomainEntity, TEventStoreEntity, TEventEntity>
        where TDomainEntity : class, IEventSourcingDomainEntity
        where TEventStoreEntity : class, IEventStoreEntity<TEventEntity>, new()
        where TEventEntity : class, IEventEntity, new()
    {
        /// <summary>
        ///     Stores the specified domain entity into the event store.
        /// </summary>
        /// <param name="domainEntity">
        ///     The domain entity to be stored.
        /// </param>
        /// <returns>
        ///     The event store entity that was persisted.
        /// </returns>
        public TEventStoreEntity Store(TDomainEntity domainEntity)
        {
            Check.NotNull(domainEntity, nameof(domainEntity));

            return StoreAndPublishEvents(domainEntity, GetEventStoreEntity(domainEntity, true));
        }

        /// <summary>
        ///     Stores the specified domain entity into the event store.
        /// </summary>
        /// <param name="domainEntity">
        ///     The domain entity to be stored.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation. The task result contains the event
        ///     store entity that was persisted.
        /// </returns>
        public async Task<TEventStoreEntity> StoreAsync(TDomainEntity domainEntity)
        {
            Check.NotNull(domainEntity, nameof(domainEntity));

            return StoreAndPublishEvents(domainEntity, await GetEventStoreEntityAsync(domainEntity, true));
        }

        /// <summary>
        ///     Removes the specified domain entity from the event store.
        /// </summary>
        /// <param name="domainEntity">
        ///     The domain entity to be removed.
        /// </param>
        /// <returns>
        ///     The event store entity that was removed.
        /// </returns>
        public TEventStoreEntity Remove(TDomainEntity domainEntity)
        {
            var eventStoreEntity = GetEventStoreEntity(domainEntity, false);

            RemoveCore(eventStoreEntity);

            return eventStoreEntity;
        }

        /// <summary>
        ///     Removes the specified domain entity from the event store.
        /// </summary>
        /// <param name="domainEntity">
        ///     The domain entity to be removed.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation. The task result contains the event
        ///     store entity that was removed.
        /// </returns>
        public async Task<TEventStoreEntity> RemoveAsync(TDomainEntity domainEntity)
        {
            var eventStoreEntity = await GetEventStoreEntityAsync(domainEntity, false);

            RemoveCore(eventStoreEntity);

            return eventStoreEntity;
        }

        /// <summary>
        ///     Returns the event store entity related to the specified domain entity.
        /// </summary>
        /// <param name="domainEntity">
        ///     The domain entity.
        /// </param>
        /// <param name="addIfNotFound">
        ///     Specifies whether the entity must be created when not found.
        /// </param>
        /// <returns>
        ///     The event store entity.
        /// </returns>
        protected virtual TEventStoreEntity GetEventStoreEntity(TDomainEntity domainEntity, bool addIfNotFound)
        {
            var eventStore = GetEventStoreEntity(domainEntity);

            return EnsureEventStoreEntityIsNotNull(eventStore, domainEntity, addIfNotFound);
        }

        /// <summary>
        ///     Returns the event store entity related to the specified domain entity.
        /// </summary>
        /// <param name="domainEntity">
        ///     The domain entity.
        /// </param>
        /// <param name="addIfNotFound">
        ///     Specifies whether the entity must be created when not found.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation. The task result contains the event
        ///     store entity.
        /// </returns>
        protected virtual async Task<TEventStoreEntity> GetEventStoreEntityAsync(
            TDomainEntity domainEntity,
            bool addIfNotFound)
        {
            var eventStore = await GetEventStoreEntityAsync(domainEntity);

            return EnsureEventStoreEntityIsNotNull(eventStore, domainEntity, addIfNotFound);
        }

        /// <summary>
        ///     Adds the new event store entity to the storage, without committing yet.
        /// </summary>
        /// <remarks>
        ///     In EF Core this equals to adding the entity to the <c>DbSet</c> without calling <c>SaveChanges</c> (that will be called later by the framework).
        /// </remarks>
        /// <param name="eventStoreEntity">
        ///     The event store entity to be added.
        /// </param>
        protected abstract void AddEventStoreEntity(TEventStoreEntity eventStoreEntity);

        /// <summary>
        ///     Returns the event store entity related to the specified domain entity.
        /// </summary>
        /// <param name="domainEntity">
        ///     The domain entity.
        /// </param>
        /// <returns>
        ///     The event store entity.
        /// </returns>
        protected abstract TEventStoreEntity? GetEventStoreEntity(TDomainEntity domainEntity);

        /// <summary>
        ///     Returns the event store entity related to the specified domain entity.
        /// </summary>
        /// <param name="domainEntity">
        ///     The domain entity.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation. The task result contains the event
        ///     store entity.
        /// </returns>
        protected abstract Task<TEventStoreEntity?> GetEventStoreEntityAsync(TDomainEntity domainEntity);

        /// <summary>
        ///     Maps the domain entity to the event store entity.
        /// </summary>
        /// <remarks>
        ///     This method should map the entity keys only. The events are handled automatically.
        /// </remarks>
        /// <param name="domainEntity">
        ///     The domain entity to be mapped.
        /// </param>
        /// <param name="eventStoreEntity">
        ///     The event store entity to be initialized after the domain entity.
        /// </param>
        protected virtual void MapEventStoreEntity(TDomainEntity domainEntity, TEventStoreEntity eventStoreEntity)
        {
            Check.NotNull(domainEntity, nameof(domainEntity));
            Check.NotNull(eventStoreEntity, nameof(eventStoreEntity));

            PropertiesMapper.Map(domainEntity, eventStoreEntity);
        }

        /// <summary>
        ///     Rebuilds the domain entity applying the stored events.
        /// </summary>
        /// <param name="eventStoreEntity">
        ///     The event store entity referencing the events to be applied.
        /// </param>
        /// <param name="snapshot">
        ///     The optional datetime of the snapshot to build. Specifying it will cause only the events up to this
        ///     datetime to be applied.
        /// </param>
        /// <returns>
        ///     The domain entity rebuilt from the stored events.
        /// </returns>
        protected virtual TDomainEntity GetDomainEntity(
            TEventStoreEntity eventStoreEntity,
            DateTime? snapshot = null)
        {
            Check.NotNull(eventStoreEntity, nameof(eventStoreEntity));

            var events = snapshot != null
                ? eventStoreEntity.Events.Where(e => e.Timestamp <= snapshot)
                : eventStoreEntity.Events;

            return EntityActivator.CreateInstance<TDomainEntity>(
                events
                    .OrderBy(e => e.Timestamp).ThenBy(e => e.Sequence)
                    .Select(MapEvent)
                    .ToList(),
                eventStoreEntity);
        }

        /// <summary>
        ///     Maps the <see cref="IEntityEvent" /> to the related entity being persisted into the underlying
        ///     storage.
        /// </summary>
        /// <param name="entityEvent">
        ///     The entity event to be mapped.
        /// </param>
        /// <returns>
        ///     The entity representing the <see cref="IEntityEvent" />.
        /// </returns>
        protected virtual TEventEntity MapEventEntity(IEntityEvent entityEvent)
        {
            Check.NotNull(entityEvent, nameof(entityEvent));

            return new TEventEntity
            {
                SerializedEvent = JsonSerializer.Serialize(entityEvent),
                ClrType = entityEvent.GetType().AssemblyQualifiedName,
                Timestamp = entityEvent.Timestamp,
                Sequence = entityEvent.Sequence
            };
        }

        /// <summary>
        ///     Maps the persisted entity back to the <see cref="IEntityEvent" />.
        /// </summary>
        /// <param name="eventEntity">
        ///     The stored event entity to be mapped.
        /// </param>
        /// <returns>
        ///     The <see cref="IEntityEvent" />.
        /// </returns>
        protected virtual IEntityEvent MapEvent(TEventEntity eventEntity)
        {
            Check.NotNull(eventEntity, nameof(eventEntity));

            IEntityEvent entityEvent;

            if (eventEntity.ClrType != null)
            {
                var eventType = TypesCache.GetType(eventEntity.ClrType);
                entityEvent = (IEntityEvent)JsonSerializer.Deserialize(eventEntity.SerializedEvent, eventType);
            }
            else
            {
                entityEvent = (IEntityEvent)PolymorphicJsonSerializer.Deserialize(eventEntity.SerializedEvent);
            }

            entityEvent.Sequence = eventEntity.Sequence;
            entityEvent.Timestamp = eventEntity.Timestamp;

            return entityEvent;
        }

        /// <summary>
        ///     Removes the event store entity and all related events from the store.
        /// </summary>
        /// <param name="eventStore">
        ///     The entity to be removed.
        /// </param>
        protected abstract void RemoveCore(TEventStoreEntity eventStore);

        private TEventStoreEntity EnsureEventStoreEntityIsNotNull(
            TEventStoreEntity? eventStore,
            TDomainEntity domainEntity,
            bool addIfNotFound)
        {
            if (eventStore == null)
            {
                if (addIfNotFound)
                {
                    eventStore = new TEventStoreEntity();
                    MapEventStoreEntity(domainEntity, eventStore);
                    AddEventStoreEntity(eventStore);
                }
                else
                {
                    throw new EventStoreNotFoundException("The event store entity could not be found.");
                }
            }

            return eventStore;
        }

        private TEventStoreEntity StoreAndPublishEvents(TDomainEntity domainEntity, TEventStoreEntity eventStore)
        {
            var newEvents = domainEntity.GetNewEvents().ToList();

            eventStore.EntityVersion += newEvents.Count;

            MapEventStoreEntity(domainEntity, eventStore);

            if (eventStore.EntityVersion != domainEntity.GetVersion())
            {
                throw new EventStoreConcurrencyException(
                    $"Expected to save version {domainEntity.GetVersion()} but new version was {eventStore.EntityVersion}. " +
                    "Refresh the domain entity and reapply the new events.");
            }

            newEvents.ForEach(entityEvent => eventStore.Events.Add(MapEventEntity(entityEvent)));

            // Move the domain events from the domain entity to the event store entity
            // being persisted, in order for Silverback to automatically publish them
            // (even if the domain entity itself is not attached to the DbContext).
            if (domainEntity is IMessagesSource messagesSource)
            {
                var messages = messagesSource.GetMessages()?.ToList();

                if (messages != null && messages.Count > 0)
                {
                    eventStore.AddDomainEvents(messages);
                    messagesSource.ClearMessages();
                }
            }

            return eventStore;
        }
    }
}
