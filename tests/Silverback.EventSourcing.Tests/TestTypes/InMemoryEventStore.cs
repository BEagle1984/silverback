// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.EventStore;

namespace Silverback.Tests.EventSourcing.TestTypes
{
    public abstract class InMemoryEventStoreRepository<TAggregateEntity, TEventStoreEntity, TEventEntity>
        : EventStoreRepository<TAggregateEntity, TEventStoreEntity, TEventEntity>
        where TAggregateEntity : IEventSourcingAggregate
        where TEventStoreEntity : IEventStoreEntity<TEventEntity>
        where TEventEntity : IEventEntity, new()
    {
        public readonly List<TEventStoreEntity> EventStores = new List<TEventStoreEntity>();

        protected override TEventStoreEntity Remove(TAggregateEntity aggregateEntity, TEventStoreEntity eventStore)
        {
            if (eventStore != null)
                EventStores.Remove(eventStore);

            return eventStore;
        }
    }
}
