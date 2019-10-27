// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.EventStore
{
    public interface IEventStoreEntity<TEventEntity>
        where TEventEntity : IEventEntity
    {
        ICollection<TEventEntity> Events { get; }

        int EntityVersion { get; set; }

        void AddDomainEvents(IEnumerable<object> events);
    }
}