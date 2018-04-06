namespace Silverback.Domain
{
    /// <summary>
    /// Represent an event published by an aggregate root or an entity.
    /// </summary>
    /// <typeparam name="T">The type of the source entity.</typeparam>
    /// <seealso cref="IDomainEvent{T}" />
    public abstract class DomainEvent<T> : IDomainEvent<T>
        where T : IDomainEntity
    {
         /// <summary>
        /// Gets the reference to the entity instance that generated this event.
        /// </summary>
        public T Source { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainEvent{T}"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        protected DomainEvent(T source)
        {
            Source = source;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainEvent{T}"/> class.
        /// </summary>
        protected DomainEvent()
        {
        }

        /// <summary>
        /// Gets the entity instance that generated this event.
        /// </summary>
        IDomainEntity IDomainEvent.Source
        {
            get => Source;
            set => Source = (T) value;
        }
    }
}
