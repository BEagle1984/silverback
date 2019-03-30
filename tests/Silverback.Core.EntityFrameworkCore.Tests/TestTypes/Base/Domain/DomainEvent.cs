// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)
namespace Silverback.Tests.Core.EntityFrameworkCore.TestTypes.Base.Domain
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
