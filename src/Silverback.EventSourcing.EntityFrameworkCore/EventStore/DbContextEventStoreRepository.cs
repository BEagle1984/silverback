// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Silverback.EventStore
{
    public abstract class DbContextEventStoreRepository<TAggregateEntity, TKey, TEventStoreEntity, TEventEntity>
        : EventStoreRepository<TAggregateEntity, TEventStoreEntity, TEventEntity>
        where TAggregateEntity : IEventSourcingAggregate<TKey>
        where TEventStoreEntity : EventStoreEntity<TEventEntity>
        where TEventEntity : IEventEntity, new()
    {
        private readonly DbSet<TEventStoreEntity> _dbSet;

        protected DbContextEventStoreRepository(DbContext dbContext)
        {
            _dbSet = dbContext.Set<TEventStoreEntity>();
        }

        protected IQueryable<TEventStoreEntity> EventStores => _dbSet.Include(s => s.Events);

        /// <summary>
        /// Gets the aggregate entity folding the events from the specified event store.
        /// </summary>
        /// <param name="predicate">The predicate applied to get the desired event store.</param>
        /// <returns></returns>
        public TAggregateEntity Get(Expression<Func<TEventStoreEntity, bool>> predicate) =>
            GetAggregateEntity(EventStores.AsNoTracking().FirstOrDefault(predicate));

        /// <summary>
        /// Gets the aggregate entity folding the events from the specified event store.
        /// </summary>
        /// <param name="predicate">The predicate applied to get the desired event store.</param>
        /// <returns></returns>
        public async Task<TAggregateEntity> GetAsync(Expression<Func<TEventStoreEntity, bool>> predicate) =>
            GetAggregateEntity(await EventStores.AsNoTracking().FirstOrDefaultAsync(predicate));

        /// <summary>
        /// Gets the aggregate entity folding the events from the specified event store.
        /// Only the events until the specified date and time are applied, giving
        /// a snapshot of a past state of the entity.
        /// </summary>
        /// <param name="predicate">The predicate applied to get the desired event store.</param>
        /// <param name="snapshot">The snapshot date and time.</param>
        /// <returns></returns>
        public TAggregateEntity GetSnapshot(Expression<Func<TEventStoreEntity, bool>> predicate, DateTime snapshot) =>
            GetAggregateEntity(EventStores.AsNoTracking().FirstOrDefault(predicate), snapshot);

        /// <summary>
        /// Gets the aggregate entity folding the events from the specified event store.
        /// Only the events until the specified date and time are applied, giving
        /// a snapshot of a past state of the entity.
        /// </summary>
        /// <param name="predicate">The predicate applied to get the desired event store.</param>
        /// <param name="snapshot">The snapshot date and time.</param>
        /// <returns></returns>
        public async Task<TAggregateEntity> GetSnapshotAsync(Expression<Func<TEventStoreEntity, bool>> predicate, DateTime snapshot) =>
            GetAggregateEntity(await EventStores.AsNoTracking().FirstOrDefaultAsync(predicate), snapshot);

        protected override TEventStoreEntity GetEventStoreEntity(TAggregateEntity aggregateEntity, bool addIfNotFound)
        {
            var eventStore = _dbSet.Find(aggregateEntity.Id);

            if (eventStore == null && addIfNotFound)
            {
                eventStore = GetNewEventStoreEntity(aggregateEntity);
                _dbSet.Add(eventStore);
            }

            return eventStore;
        }

        protected override async Task<TEventStoreEntity> GetEventStoreEntityAsync(TAggregateEntity aggregateEntity, bool addIfNotFound)
        {
            var eventStore = await _dbSet.FindAsync(aggregateEntity.Id);

            if (eventStore == null && addIfNotFound)
            {
                eventStore = GetNewEventStoreEntity(aggregateEntity);
                _dbSet.Add(eventStore);
            }

            return eventStore;
        }

        protected override TEventStoreEntity Remove(TAggregateEntity aggregateEntity, TEventStoreEntity eventStore)
        {
            if (eventStore == null)
                return default;

            return _dbSet.Remove(eventStore)?.Entity;
        }

        protected abstract TEventStoreEntity GetNewEventStoreEntity(TAggregateEntity aggregateEntity);
    }
}
