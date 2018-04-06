using System.Collections.Generic;

namespace Silverback.Domain
{
    /// <summary>
    /// A base class for the domain entities implementing <see cref="IDomainEntity"/>.
    /// </summary>
    public abstract class Entity : IDomainEntity
    {
        private List<IDomainEvent<IDomainEntity>> _domainEvents;

        /// <summary>
        /// Gets the domain events published by this entity.
        /// </summary>
        /// <remarks></remarks>
        public IEnumerable<IDomainEvent<IDomainEntity>> GetDomainEvents() => _domainEvents?.AsReadOnly();

        /// <summary>
        /// Clears all domain events.
        /// </summary>
        public void ClearEvents()
        {
            _domainEvents?.Clear();
        }

        /// <summary>
        /// Adds the specified domain event.
        /// </summary>
        /// <param name="domainEvent">The domain event.</param>
        protected void AddEvent(IDomainEvent<IDomainEntity> domainEvent)
        {
            if (_domainEvents == null)
                _domainEvents = new List<IDomainEvent<IDomainEntity>>();

            ((IDomainEvent)domainEvent).Source = this;

            _domainEvents.Add(domainEvent);
        }

        /// <summary>
        /// Adds a new event of the specified type.
        /// </summary>
        /// <typeparam name="TEvent">The type of the event.</typeparam>
        /// <returns></returns>
        protected TEvent AddEvent<TEvent>()
            where TEvent : IDomainEvent<IDomainEntity>, new()
        {
            var evnt = new TEvent();
            AddEvent(evnt);
            return evnt;
        }

        /// <summary>
        /// Removes the specified domain event.
        /// </summary>
        /// <param name="domainEvent">The domain event.</param>
        protected void RemoveEvent(IDomainEvent<Entity> domainEvent)
        {
            _domainEvents?.Remove(domainEvent);
        }
    }
}
