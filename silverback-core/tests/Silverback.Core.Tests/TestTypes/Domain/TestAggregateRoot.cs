using Silverback.Domain;

namespace Silverback.Tests.TestTypes.Domain
{
    public class TestAggregateRoot : DomainEntity, IAggregateRoot
    {
        public new void AddEvent(IDomainEvent<IDomainEntity> domainEvent)
            => base.AddEvent(domainEvent);

        public new TEvent AddEvent<TEvent>() 
            where TEvent : IDomainEvent<IDomainEntity>, new()
            => base.AddEvent<TEvent>();
    }
}
