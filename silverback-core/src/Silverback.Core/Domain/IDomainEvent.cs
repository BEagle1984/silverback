using Silverback.Messaging.Messages;

namespace Silverback.Domain
{
    /// <summary>
    /// Represent an event published by an aggregate root or an entity.
    /// </summary>
    /// <typeparam name="T">The type of the source entity.</typeparam>
    /// <seealso cref="IEvent" />
    public interface IDomainEvent : IEvent
    {
        /// <summary>
        /// Gets or sets the entity instance that generated this event.
        /// </summary>
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
        /// <summary>
        /// Gets the entity instance that generated this event.
        /// </summary>
        new T Source { get; }
    }
}