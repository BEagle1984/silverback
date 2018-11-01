using System.Collections.Generic;

namespace Silverback.Domain
{
    /// <summary>
    /// A sample implementation of <see cref="IDomainEntity"/>.
    /// </summary>
    public abstract class Entity : IDomainEntity
    {
        private List<IDomainEvent<IDomainEntity>> _domainEvents;

        public IEnumerable<IDomainEvent<IDomainEntity>> GetDomainEvents() => _domainEvents?.AsReadOnly();

        public void ClearEvents() => _domainEvents?.Clear();

        protected void AddEvent(IDomainEvent<IDomainEntity> domainEvent)
        {
            if (_domainEvents == null)
                _domainEvents = new List<IDomainEvent<IDomainEntity>>();

            ((IDomainEvent)domainEvent).Source = this;

            _domainEvents.Add(domainEvent);
        }

        protected TEvent AddEvent<TEvent>()
            where TEvent : IDomainEvent<IDomainEntity>, new()
        {
            var evnt = new TEvent();
            AddEvent(evnt);
            return evnt;
        }

        protected void RemoveEvent(IDomainEvent<Entity> domainEvent) => _domainEvents?.Remove(domainEvent);
    }
}
