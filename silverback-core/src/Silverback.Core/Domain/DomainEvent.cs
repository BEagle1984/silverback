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
        public T Source { get; set; }

        protected DomainEvent(T source)
        {
            Source = source;
        }

        protected DomainEvent()
        {
        }

        IDomainEntity IDomainEvent.Source
        {
            get => Source;
            set => Source = (T) value;
        }
    }
}
