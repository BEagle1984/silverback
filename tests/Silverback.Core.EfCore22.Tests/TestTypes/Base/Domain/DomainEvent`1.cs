// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Tests.Core.EFCore22.TestTypes.Base.Domain
{
    public abstract class DomainEvent<TEntity> : IDomainEvent<TEntity>
        where TEntity : class
    {
        public TEntity? Source { get; set; }

        object? IDomainEvent.Source
        {
            get => Source;
            set => Source = (TEntity?)value;
        }
    }
}
