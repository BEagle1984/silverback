using Silverback.Messaging.Messages;

namespace Silverback.Domain
{
    public interface IDomainEvent : IEvent
    {
        IDomainEntity Source { get; set; }
    }

    public interface IDomainEvent<out TEntity> : IDomainEvent
        where TEntity : IDomainEntity
    {
        new TEntity Source { get; }
    }
}