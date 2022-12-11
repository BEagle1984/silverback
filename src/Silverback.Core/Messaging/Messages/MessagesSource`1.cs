// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;

namespace Silverback.Messaging.Messages;

/// <summary>
///     The default generic implementation of <see cref="IMessagesSource" />. It contains some protected
///     methods to add the internal events to a temporary collection exposed via the
///     <see cref="IMessagesSource" /> implementation.
/// </summary>
/// <remarks>
///     This is the base class of the <c>DomainEntity</c> defined in Silverback.Core.Model.
/// </remarks>
/// <typeparam name="TBaseEvent">
///     The base type of the events being published.
/// </typeparam>
public abstract class MessagesSource<TBaseEvent> : IMessagesSource
{
    private List<TBaseEvent>? _events;

    /// <inheritdoc cref="IMessagesSource.GetMessages" />
    public IEnumerable<object>? GetMessages() => _events?.Cast<object>();

    /// <inheritdoc cref="IMessagesSource.ClearMessages" />
    public void ClearMessages() => _events?.Clear();

    /// <summary>
    ///     <para>
    ///         Adds the specified event to the collection of events related to this object.
    ///     </para>
    ///     <para>
    ///         In the case of an entity model the event will be published when the entity is saved to the
    ///         underlying database.
    ///     </para>
    /// </summary>
    /// <param name="event">
    ///     The instance of <typeparamref name="TBaseEvent" /> to be added.
    /// </param>
    protected virtual void AddEvent(TBaseEvent @event)
    {
        _events ??= new List<TBaseEvent>();

        if (@event is IMessageWithSource messageWithSource)
            messageWithSource.Source = this;

        _events.Add(@event);
    }

    /// <summary>
    ///     <para>
    ///         Adds a new instance of <typeparamref name="TEvent" /> to the collection of events related to
    ///         this object.
    ///     </para>
    ///     <para>
    ///         In the case of an entity model the event will be published when the entity is saved to the
    ///         underlying database.
    ///     </para>
    /// </summary>
    /// <typeparam name="TEvent">
    ///     The type of the event.
    /// </typeparam>
    /// <param name="allowMultiple">
    ///     if set to <c>false</c> only one instance of the specified type <c>TEvent</c> will be added.
    /// </param>
    /// <returns>
    ///     The <typeparamref name="TEvent" /> instance that was added.
    /// </returns>
    protected TEvent AddEvent<TEvent>(bool allowMultiple = true)
        where TEvent : TBaseEvent, new()
    {
        if (!allowMultiple && _events != null && _events.OfType<TEvent>().Any())
            return _events.OfType<TEvent>().First();

        TEvent @event = new();
        AddEvent(@event);
        return @event;
    }

    /// <summary>
    ///     Removes the specified event from the collection of events related to this object.
    /// </summary>
    /// <remarks>
    ///     This is used only to withdraw an event that wasn't published yet.
    /// </remarks>
    /// <param name="event">
    ///     The <typeparamref name="TBaseEvent" /> to be removed.
    /// </param>
    protected void RemoveEvent(TBaseEvent @event) => _events?.Remove(@event);
}
