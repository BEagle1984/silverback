﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Silverback.EventStore;

namespace Silverback.Tests.EventSourcing.TestTypes;

[SuppressMessage("", "CA1005", Justification = Justifications.NoWayToReduceTypeParameters)]
public abstract class InMemoryEventStoreRepository<TAggregateEntity, TEventStoreEntity, TEventEntity>
    : EventStoreRepository<TAggregateEntity, TEventStoreEntity, TEventEntity>
    where TAggregateEntity : class, IEventSourcingDomainEntity
    where TEventStoreEntity : class, IEventStoreEntity<TEventEntity>, new()
    where TEventEntity : class, IEventEntity, new()
{
    public IList<TEventStoreEntity> EventStores { get; } = new List<TEventStoreEntity>();

    protected override void RemoveCore(TEventStoreEntity eventStore) => EventStores.Remove(eventStore);
}
