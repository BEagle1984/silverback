using Silverback.Messaging.Messages;

namespace Silverback.Domain
{
    /// <summary>
    /// Represent an event published by an aggregate root or an entity.
    /// </summary>
    /// <seealso cref="IEvent" />
    public interface IDomainEvent : IEvent
    {
        IDomainEntity Source { get; set; }
    }

    /// <summary>
    /// Represent an event published by an aggregate root or an entity.
    /// </summary>
    /// <typeparam name="T">The type of the source entity.</typeparam>
    /// <seealso cref="IEvent" />
    public interface IDomainEvent<out T> : IDomainEvent
        where T : IDomainEntity
    {
        new T Source { get; }
    }
}