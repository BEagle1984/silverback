// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.EventStore;

/// <summary>
///     The stored entity that contain/references all the events applied to a domain entity.
/// </summary>
/// <typeparam name="TEventEntity">
///     The type of the related event entity that will be referenced.
/// </typeparam>
public interface IEventStoreEntity<TEventEntity>
    where TEventEntity : IEventEntity
{
    /// <summary>
    ///     Gets the events that have been applied to the domain entity.
    /// </summary>
    ICollection<TEventEntity> Events { get; }

    /// <summary>
    ///     Gets or sets the version of the entity.
    /// </summary>
    /// <remarks>
    ///     In the default implementation this is a sequence that is increment every time a new event is
    ///     applied.
    /// </remarks>
    int EntityVersion { get; set; }

    /// <summary>
    ///     Adds the specified events.
    /// </summary>
    /// <param name="events">
    ///     The events to be stored.
    /// </param>
    void AddDomainEvents(IEnumerable<object> events);
}
