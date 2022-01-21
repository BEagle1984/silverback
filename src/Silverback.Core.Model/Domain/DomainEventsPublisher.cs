// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Domain;

/// <summary>
///     Publishes the domain events stored into the domain entities. Can be plugged into an Entity Framework's DbContext to automatically
///     publish the events related to the modified entities.
/// </summary>
public class DomainEventsPublisher
{
    private readonly Func<IEnumerable<object>> _entitiesProvider;

    private readonly Action<object> _clearEventsAction;

    private readonly Func<object, IEnumerable<object>?> _eventsSelector;

    private readonly IPublisher _publisher;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DomainEventsPublisher" /> class.
    /// </summary>
    /// <param name="entitiesProvider">
    ///     The function returning the modified entities to be scanned for domain events.
    /// </param>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" /> to be used to publish the events to the internal bus.
    /// </param>
    public DomainEventsPublisher(Func<IEnumerable<object>> entitiesProvider, IPublisher publisher)
        : this(
            entitiesProvider,
            entity => (entity as IMessagesSource)?.GetMessages(),
            entity => (entity as IMessagesSource)?.ClearMessages(),
            publisher)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DomainEventsPublisher" /> class.
    /// </summary>
    /// <param name="entitiesProvider">
    ///     The function returning the modified entities to be scanned for domain events.
    /// </param>
    /// <param name="eventsSelector">
    ///     The custom delegate to be used to get the events out of the entities being saved.
    /// </param>
    /// <param name="clearEventsAction">
    ///     The custom delegate to be used to clear the events from the entities after they have been published.
    /// </param>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" /> to be used to publish the events to the internal bus.
    /// </param>
    public DomainEventsPublisher(
        Func<IEnumerable<object>> entitiesProvider,
        Func<object, IEnumerable<object>?> eventsSelector,
        Action<object> clearEventsAction,
        IPublisher publisher)
    {
        _entitiesProvider = Check.NotNull(entitiesProvider, nameof(entitiesProvider));
        _eventsSelector = Check.NotNull(eventsSelector, nameof(eventsSelector));
        _clearEventsAction = Check.NotNull(clearEventsAction, nameof(clearEventsAction));
        _publisher = Check.NotNull(publisher, nameof(publisher));
    }

    /// <summary>
    ///     Publishes the domain events stored into the domain entities returned by the provider function.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public async Task PublishDomainEventsAsync() => await PublishDomainEventsAsync(true).ConfigureAwait(false);

    /// <summary>
    ///     Publishes the domain events stored into the domain entities returned by the provider function.
    /// </summary>
    public void PublishDomainEvents() => PublishDomainEventsAsync(false).GetAwaiter().GetResult();

    [SuppressMessage("", "VSTHRD103", Justification = Justifications.ExecutesSyncOrAsync)]
    private async Task PublishDomainEventsAsync(bool executeAsync)
    {
        IReadOnlyCollection<object> events = GetDomainEvents();

        // Keep publishing events fired inside the event handlers
        while (events.Any())
        {
            if (executeAsync)
            {
                await events.ForEachAsync(message => _publisher.PublishAsync(message))
                    .ConfigureAwait(false);
            }
            else
            {
                events.ForEach(message => _publisher.Publish(message));
            }

            events = GetDomainEvents();
        }
    }

    private IReadOnlyCollection<object> GetDomainEvents() =>
        _entitiesProvider.Invoke().SelectMany(
            entity =>
            {
                List<object>? selected = _eventsSelector(entity)?.ToList();

                // Clear all events to avoid firing the same event multiple times during the recursion
                _clearEventsAction(entity);

                return selected ?? Enumerable.Empty<object>();
            }).ToList();

    [SuppressMessage("", "VSTHRD103", Justification = Justifications.ExecutesSyncOrAsync)]
    private async Task PublishEventAsync<TEvent>(bool executeAsync)
        where TEvent : new()
    {
        if (executeAsync)
            await _publisher.PublishAsync(new TEvent()).ConfigureAwait(false);
        else
            _publisher.Publish(new TEvent());
    }
}
