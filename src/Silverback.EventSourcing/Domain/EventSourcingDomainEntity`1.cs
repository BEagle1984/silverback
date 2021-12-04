// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.EventStore;

namespace Silverback.Domain;

/// <summary>
///     The base class for the domain entities that are persisted in the event store.
/// </summary>
/// <remarks>
///     It's not mandatory to use this base class as long as long as the domain entities implement the
///     <see cref="IEventSourcingDomainEntity{TKey}" /> interface.
/// </remarks>
/// <typeparam name="TKey">
///     The type of the entity key.
/// </typeparam>
public abstract class EventSourcingDomainEntity<TKey> : EventSourcingDomainEntity<TKey, object>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="EventSourcingDomainEntity{TKey}" /> class.
    /// </summary>
    protected EventSourcingDomainEntity()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EventSourcingDomainEntity{TKey}" /> class from the
    ///     stored events.
    /// </summary>
    /// <param name="events">
    ///     The stored events to be re-applied to rebuild the entity state.
    /// </param>
    protected EventSourcingDomainEntity(IReadOnlyCollection<IEntityEvent> events)
        : base(events)
    {
    }
}
