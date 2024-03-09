// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Publishing;

/// <summary>
///     Adds the <c>PublishEvent</c>, <c>PublishEvents</c>, <c>PublishEventAsync</c> and <c>PublishEventsAsync</c> methods to the
///     <see cref="IPublisher" /> interface.
/// </summary>
public static class EventPublisherExtensions
{
    /// <summary>
    ///     Publishes the specified event to the internal bus. The message will be forwarded to its subscribers and the method will not
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
    ///     Publishes the specified event to the internal bus. The message will be forwarded to its subscribers and the method will not
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
    public static void PublishEvent(this IPublisher publisher, IMessageWrapper<IEvent> eventMessage, bool throwIfUnhandled = false) =>
        Check.NotNull(publisher, nameof(publisher)).Publish(eventMessage, throwIfUnhandled);

    /// <summary>
    ///     Publishes the specified events to the internal bus. The messages will be forwarded to its subscribers and the method will not
    ///     complete until all subscribers have processed them (unless using Silverback.Integration to produce and consume the messages
    ///     through a message broker).
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="eventMessages">
    ///     The events to be published.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the messages.
    /// </param>
    public static void PublishEvents(this IPublisher publisher, IEnumerable<IEvent> eventMessages, bool throwIfUnhandled = false) =>
        Check.NotNull(publisher, nameof(publisher)).Publish(eventMessages, throwIfUnhandled);

    /// <summary>
    ///     Publishes the specified events to the internal bus. The messages will be forwarded to its subscribers and the method will not
    ///     complete until all subscribers have processed them (unless using Silverback.Integration to produce and consume the messages
    ///     through a message broker).
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="eventMessages">
    ///     The events to be published.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the messages.
    /// </param>
    public static void PublishEvents(
        this IPublisher publisher,
        IEnumerable<IMessageWrapper<IEvent>> eventMessages,
        bool throwIfUnhandled = false) =>
        Check.NotNull(publisher, nameof(publisher)).Publish(eventMessages, throwIfUnhandled);

    /// <summary>
    ///     Publishes the specified events to the internal bus. The messages will be forwarded to its subscribers and the method will not
    ///     complete until all subscribers have processed them (unless using Silverback.Integration to produce and consume the messages
    ///     through a message broker).
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="eventMessages">
    ///     The events to be published.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the messages.
    /// </param>
    public static void PublishEvents(this IPublisher publisher, IAsyncEnumerable<IEvent> eventMessages, bool throwIfUnhandled = false) =>
        Check.NotNull(publisher, nameof(publisher)).Publish(eventMessages, throwIfUnhandled);

    /// <summary>
    ///     Publishes the specified events to the internal bus. The messages will be forwarded to its subscribers and the method will not
    ///     complete until all subscribers have processed them (unless using Silverback.Integration to produce and consume the messages
    ///     through a message broker).
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="eventMessages">
    ///     The events to be published.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the messages.
    /// </param>
    public static void PublishEvents(
        this IPublisher publisher,
        IAsyncEnumerable<IMessageWrapper<IEvent>> eventMessages,
        bool throwIfUnhandled = false) =>
        Check.NotNull(publisher, nameof(publisher)).Publish(eventMessages, throwIfUnhandled);

    /// <summary>
    ///     Publishes the specified event to the internal bus. The message will be forwarded to its subscribers and the <see cref="ValueTask" />
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
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    public static ValueTask PublishEventAsync(this IPublisher publisher, IEvent eventMessage, bool throwIfUnhandled = false) =>
        Check.NotNull(publisher, nameof(publisher)).PublishAsync(eventMessage, throwIfUnhandled);

    /// <summary>
    ///     Publishes the specified event to the internal bus. The message will be forwarded to its subscribers and the <see cref="ValueTask" />
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
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    public static ValueTask PublishEventAsync(this IPublisher publisher, IMessageWrapper<IEvent> eventMessage, bool throwIfUnhandled = false) =>
        Check.NotNull(publisher, nameof(publisher)).PublishAsync(eventMessage, throwIfUnhandled);

    /// <summary>
    ///     Publishes the specified events to the internal bus. The messages will be forwarded to its subscribers and the <see cref="ValueTask" />
    ///     will not complete until all subscribers have processed them (unless using Silverback.Integration to produce and consume the messages
    ///     through a message broker).
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="eventMessages">
    ///     The events to be published.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the messages.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    public static ValueTask PublishEventsAsync(this IPublisher publisher, IEnumerable<IEvent> eventMessages, bool throwIfUnhandled = false) =>
        Check.NotNull(publisher, nameof(publisher)).PublishAsync(eventMessages, throwIfUnhandled);

    /// <summary>
    ///     Publishes the specified events to the internal bus. The messages will be forwarded to its subscribers and the <see cref="ValueTask" />
    ///     will not complete until all subscribers have processed them (unless using Silverback.Integration to produce and consume the messages
    ///     through a message broker).
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="eventMessages">
    ///     The events to be published.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the messages.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    public static ValueTask PublishEventsAsync(
        this IPublisher publisher,
        IEnumerable<IMessageWrapper<IEvent>> eventMessages,
        bool throwIfUnhandled = false) =>
        Check.NotNull(publisher, nameof(publisher)).PublishAsync(eventMessages, throwIfUnhandled);

    /// <summary>
    ///     Publishes the specified events to the internal bus. The messages will be forwarded to its subscribers and the <see cref="ValueTask" />
    ///     will not complete until all subscribers have processed them (unless using Silverback.Integration to produce and consume the messages
    ///     through a message broker).
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="eventMessages">
    ///     The events to be published.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the messages.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    public static ValueTask PublishEventsAsync(
        this IPublisher publisher,
        IAsyncEnumerable<IEvent> eventMessages,
        bool throwIfUnhandled = false) =>
        Check.NotNull(publisher, nameof(publisher)).PublishAsync(eventMessages, throwIfUnhandled);

    /// <summary>
    ///     Publishes the specified events to the internal bus. The messages will be forwarded to its subscribers and the <see cref="ValueTask" />
    ///     will not complete until all subscribers have processed them (unless using Silverback.Integration to produce and consume the messages
    ///     through a message broker).
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="eventMessages">
    ///     The events to be published.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the messages.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    public static ValueTask PublishEventsAsync(
        this IPublisher publisher,
        IAsyncEnumerable<IMessageWrapper<IEvent>> eventMessages,
        bool throwIfUnhandled = false) =>
        Check.NotNull(publisher, nameof(publisher)).PublishAsync(eventMessages, throwIfUnhandled);
}
