// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Domain
{
    public abstract class DomainEvent<TEntity> : IDomainEvent<TEntity>
    {
        public TEntity Source { get; set; }

        object IMessageWithSource.Source
        {
            get => Source;
            set => Source = (TEntity) value;
        }
    }
}
