// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.EventStore
{
    /// <summary> A domain entity that is persisted in the event store. </summary>
    /// <typeparam name="TKey"> The type of the entity key. </typeparam>
    public interface IEventSourcingDomainEntity<out TKey> : IEventSourcingDomainEntity
    {
        /// <summary> Gets the entity identifier. </summary>
        TKey Id { get; }
    }
}
