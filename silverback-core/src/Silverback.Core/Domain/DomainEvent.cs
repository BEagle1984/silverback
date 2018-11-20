namespace Silverback.Domain
{
    public abstract class DomainEvent<TEntity> : IDomainEvent<TEntity>
        where TEntity : IDomainEntity
    {
        public TEntity Source { get; set; }

        protected DomainEvent(TEntity source)
        {
            Source = source;
        }

        protected DomainEvent()
        {
        }

        IDomainEntity IDomainEvent.Source
        {
            get => Source;
            set => Source = (TEntity) value;
        }
    }
}
