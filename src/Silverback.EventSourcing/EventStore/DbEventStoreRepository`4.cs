// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Silverback.Database;
using Silverback.Util;

namespace Silverback.EventStore
{
    /// <summary>
    ///     The base class for the event store repositories that persist the events into a database.
    /// </summary>
    /// <typeparam name="TDomainEntity">
    ///     The type of the domain entity whose events are stored in this repository.
    /// </typeparam>
    /// <typeparam name="TKey">
    ///     The type of the domain entity key.
    /// </typeparam>
    /// <typeparam name="TEventStoreEntity">
    ///     The type of event store entity being persisted to the underlying storage.
    /// </typeparam>
    /// <typeparam name="TEventEntity">
    ///     The base type of the events that will be associated to the event store entity.
    /// </typeparam>
    public abstract class DbEventStoreRepository<TDomainEntity, TKey, TEventStoreEntity, TEventEntity>
        : EventStoreRepository<TDomainEntity, TEventStoreEntity, TEventEntity>
        where TDomainEntity : class, IEventSourcingDomainEntity<TKey>
        where TEventStoreEntity : EventStoreEntity<TEventEntity>, new()
        where TEventEntity : class, IEventEntity, new()
    {
        private readonly IDbSet<TEventStoreEntity> _dbSet;

        /// <summary>
        ///     Initializes a new instance of the
        ///     <see cref="DbEventStoreRepository{TAggregateEntity, TKey, TEventStoreEntity, TEventEntity}" />
        ///     class.
        /// </summary>
        /// <param name="dbContext">
        ///     The <see cref="IDbContext" /> to use as storage.
        /// </param>
        protected DbEventStoreRepository(IDbContext dbContext)
        {
            Check.NotNull(dbContext, nameof(dbContext));

            _dbSet = dbContext.GetDbSet<TEventStoreEntity>();
        }

        /// <summary>
        ///     Gets the <see cref="IQueryable{T}" /> of event store entities.
        /// </summary>
        /// <remarks>
        ///     This <see cref="IQueryable{T}" /> is pre-configured to include the events and is meant for read only
        ///     (changes are not being tracked).
        /// </remarks>
        protected IQueryable<TEventStoreEntity> EventStores =>
            _dbSet.AsQueryable().Include(s => s.Events).AsNoTracking();

        /// <summary>
        ///     Finds the event store matching the specified predicate and if found returns the domain entity after
        ///     having applied the stored events.
        /// </summary>
        /// <param name="predicate">
        ///     The predicate applied to get the desired event store.
        /// </param>
        /// <param name="snapshot">
        ///     The optional snapshot datetime. When not <c>null</c> only the events registered until the specified datetime are applied, returning the entity in
        ///     its state back in that moment.
        /// </param>
        /// <returns>
        ///     The domain entity or <c>null</c> if not found.
        /// </returns>
        public TDomainEntity? Find(Expression<Func<TEventStoreEntity, bool>> predicate, DateTime? snapshot = null)
        {
            var eventStoreEntity = EventStores.FirstOrDefault(predicate);

            if (eventStoreEntity == null)
                return null;

            return GetDomainEntity(eventStoreEntity, snapshot);
        }

        /// <summary>
        ///     Finds the event store matching the specified predicate and if found returns the domain entity after
        ///     having applied the stored events.
        /// </summary>
        /// <param name="predicate">
        ///     The predicate applied to get the desired event store.
        /// </param>
        /// <param name="snapshot">
        ///     The optional snapshot datetime. When not <c>null</c> only the events registered until the specified datetime are applied, returning the entity in
        ///     its state back in that moment.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation. The task result contains the domain
        ///     entity or <c>null</c> if not found.
        /// </returns>
        public async Task<TDomainEntity?> FindAsync(
            Expression<Func<TEventStoreEntity, bool>> predicate,
            DateTime? snapshot = null)
        {
            var eventStoreEntity = await EventStores.FirstOrDefaultAsync(predicate).ConfigureAwait(false);

            if (eventStoreEntity == null)
                return null;

            return GetDomainEntity(eventStoreEntity, snapshot);
        }

        /// <inheritdoc cref="EventStoreRepository{TDomainEntity,TEventStoreEntity,TEventEntity}.GetEventStoreEntity(TDomainEntity)" />
        protected override TEventStoreEntity? GetEventStoreEntity(TDomainEntity domainEntity)
        {
            Check.NotNull(domainEntity, nameof(domainEntity));

            if (domainEntity.Id == null)
                throw new InvalidOperationException("The domain entity Id property is null.");

            return _dbSet.Find(domainEntity.Id);
        }

        /// <inheritdoc cref="EventStoreRepository{TDomainEntity,TEventStoreEntity,TEventEntity}.GetEventStoreEntityAsync(TDomainEntity)" />
        protected override async Task<TEventStoreEntity?> GetEventStoreEntityAsync(TDomainEntity domainEntity)
        {
            Check.NotNull(domainEntity, nameof(domainEntity));

            if (domainEntity.Id == null)
                throw new InvalidOperationException("The domain entity Id property is null.");

            return await _dbSet.FindAsync(domainEntity.Id).ConfigureAwait(false);
        }

        /// <inheritdoc cref="EventStoreRepository{TDomainEntity,TEventStoreEntity,TEventEntity}.AddEventStoreEntity" />
        protected override void AddEventStoreEntity(TEventStoreEntity eventStoreEntity) => _dbSet.Add(eventStoreEntity);

        /// <inheritdoc cref="EventStoreRepository{TDomainEntity,TEventStoreEntity,TEventEntity}.RemoveCore" />
        protected override void RemoveCore(TEventStoreEntity eventStore) => _dbSet.Remove(eventStore);
    }
}
