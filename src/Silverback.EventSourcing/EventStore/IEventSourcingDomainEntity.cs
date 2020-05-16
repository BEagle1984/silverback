// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Domain;

namespace Silverback.EventStore
{
    /// <summary> A domain entity that is persisted in the event store. </summary>
    public interface IEventSourcingDomainEntity
    {
        /// <summary>
        ///     Returns the version of the entity. In the default implementation this is a sequence that is
        ///     increment every time a new event is applied.
        /// </summary>
        /// <returns> The entity version. </returns>
        int GetVersion();

        /// <summary> Returns the new events that have to be persisted. </summary>
        /// <returns> The new events to be persisted. </returns>
        IEnumerable<IEntityEvent> GetNewEvents();
    }
}
