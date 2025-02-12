// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Publishing;

/// <summary>
///     Adds the <see cref="PublishEvent" /> and <see cref="PublishEventAsync(IPublisher,IEvent,CancellationToken)" /> methods to the <see cref="IPublisher" /> interface.
/// </summary>
public static class EventPublisherExtensions
{
    /// <summary>
    ///     Publishes the specified event to its subscribers via the mediator and the method will not
    ///     complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume the message
    ///     through a message broker).
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="eventMessage">
    ///     The event to be published.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    public static void PublishEvent(this IPublisher publisher, IEvent eventMessage, bool throwIfUnhandled = false) =>
        Check.NotNull(publisher, nameof(publisher)).Publish(eventMessage, throwIfUnhandled);

    /// <summary>
    ///     Publishes the specified event to its subscribers via the mediator and the <see cref="Task" />
    ///     will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume the message
    ///     through a message broker).
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="eventMessage">
    ///     The event to be published.
    /// </param>
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public static Task PublishEventAsync(this IPublisher publisher, IEvent eventMessage, CancellationToken cancellationToken = default) =>
        Check.NotNull(publisher, nameof(publisher)).PublishAsync(eventMessage, false, cancellationToken);

    /// <summary>
    ///     Publishes the specified event to its subscribers via the mediator and the <see cref="Task" />
    ///     will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume the message
    ///     through a message broker).
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="eventMessage">
    ///     The event to be published.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    public static Task PublishEventAsync(
        this IPublisher publisher,
        IEvent eventMessage,
        bool throwIfUnhandled,
        CancellationToken cancellationToken = default) =>
        Check.NotNull(publisher, nameof(publisher)).PublishAsync(eventMessage, throwIfUnhandled, cancellationToken);
}
