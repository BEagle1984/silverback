// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Silverback.Database;

namespace Silverback.EventStore
{
    public abstract class DbEventStoreRepository<TAggregateEntity, TKey, TEventStoreEntity, TEventEntity>
        : EventStoreRepository<TAggregateEntity, TEventStoreEntity, TEventEntity>
        where TAggregateEntity : IEventSourcingAggregate<TKey>
        where TEventStoreEntity : EventStoreEntity<TEventEntity>
        where TEventEntity : IEventEntity, new()
    {
        private readonly IDbSet<TEventStoreEntity> _dbSet;

        protected DbEventStoreRepository(IDbContext dbContext)
        {
            if (dbContext == null) throw new ArgumentNullException(nameof(dbContext));

            _dbSet = dbContext.GetDbSet<TEventStoreEntity>();
        }

        protected IQueryable<TEventStoreEntity> EventStores =>
            _dbSet.AsQueryable().Include(s => s.Events).AsNoTracking();

        /// <summary>
        ///     Gets the aggregate entity folding the events from the specified event store.
        /// </summary>
        /// <param name="predicate">The predicate applied to get the desired event store.</param>
        /// <returns></returns>
        public TAggregateEntity Get(Expression<Func<TEventStoreEntity, bool>> predicate) =>
            GetAggregateEntity(EventStores.FirstOrDefault(predicate));

        /// <summary>
        ///     Gets the aggregate entity folding the events from the specified event store.
        /// </summary>
        /// <param name="predicate">The predicate applied to get the desired event store.</param>
        /// <returns></returns>
        public async Task<TAggregateEntity> GetAsync(Expression<Func<TEventStoreEntity, bool>> predicate) =>
            GetAggregateEntity(await EventStores.FirstOrDefaultAsync(predicate));

        /// <summary>
        ///     Gets the aggregate entity folding the events from the specified event store.
        ///     Only the events until the specified date and time are applied, giving
        ///     a snapshot of a past state of the entity.
        /// </summary>
        /// <param name="predicate">The predicate applied to get the desired event store.</param>
        /// <param name="snapshot">The snapshot date and time.</param>
        /// <returns></returns>
        public TAggregateEntity GetSnapshot(Expression<Func<TEventStoreEntity, bool>> predicate, DateTime snapshot) =>
            GetAggregateEntity(EventStores.FirstOrDefault(predicate), snapshot);

        /// <summary>
        ///     Gets the aggregate entity folding the events from the specified event store.
        ///     Only the events until the specified date and time are applied, giving
        ///     a snapshot of a past state of the entity.
        /// </summary>
        /// <param name="predicate">The predicate applied to get the desired event store.</param>
        /// <param name="snapshot">The snapshot date and time.</param>
        /// <returns></returns>
        public async Task<TAggregateEntity> GetSnapshotAsync(
            Expression<Func<TEventStoreEntity, bool>> predicate,
            DateTime snapshot) =>
            GetAggregateEntity(await EventStores.FirstOrDefaultAsync(predicate), snapshot);

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

        protected override async Task<TEventStoreEntity> GetEventStoreEntityAsync(
            TAggregateEntity aggregateEntity,
            bool addIfNotFound)
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

            return _dbSet.Remove(eventStore);
        }

        protected abstract TEventStoreEntity GetNewEventStoreEntity(TAggregateEntity aggregateEntity);
    }
}