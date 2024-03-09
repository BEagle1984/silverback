// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Messaging.Publishing;

/// <summary>
///     Publishes the messages to the internal bus. This is the actual mediator that forwards the messages being published to the subscribers.
/// </summary>
public interface IPublisher
{
    /// <summary>
    ///     Gets the <see cref="SilverbackContext" /> in the current scope.
    /// </summary>
    public SilverbackContext Context { get; }

    /// <summary>
    ///     Publishes the specified message to the internal bus. The message will be forwarded to its
    ///     subscribers and the method will not complete until all subscribers have processed it (unless using
    ///     Silverback.Integration to produce and consume the message through a message broker).
    /// </summary>
    /// <param name="message">
    ///     The message to be published.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the
    ///     message.
    /// </param>
    void Publish(object message, bool throwIfUnhandled = false);

    /// <summary>
    ///     Publishes the specified message to the internal bus. The message will be forwarded to its
    ///     subscribers and the method will not complete until all subscribers have processed it (unless using
    ///     Silverback.Integration to produce and consume the message through a message broker).
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result that is expected to be returned by the subscribers.
    /// </typeparam>
    /// <param name="message">
    ///     The message to be published.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the
    ///     message.
    /// </param>
    /// <returns>
    ///     A collection of <typeparamref name="TResult" />, since multiple subscribers could handle the message
    ///     and return a value.
    /// </returns>
    IReadOnlyCollection<TResult> Publish<TResult>(object message, bool throwIfUnhandled = false);

    /// <summary>
    ///     Publishes the specified message to the internal bus. The message will be forwarded to its
    ///     subscribers and the <see cref="Task" /> will not complete until all subscribers have processed it
    ///     (unless using Silverback.Integration to produce and consume the message through a message broker).
    /// </summary>
    /// <param name="message">
    ///     The message to be published.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the
    ///     message.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    ValueTask PublishAsync(object message, bool throwIfUnhandled = false);

    /// <summary>
    ///     Publishes the specified message to the internal bus. The message will be forwarded to its
    ///     subscribers and the <see cref="Task" /> will not complete until all subscribers have processed it
    ///     (unless using Silverback.Integration to produce and consume the message through a message broker).
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result that is expected to be returned by the subscribers.
    /// </typeparam>
    /// <param name="message">
    ///     The message to be published.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the
    ///     message.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The task result contains a
    ///     collection of <typeparamref name="TResult" />, since multiple subscribers could handle the message
    ///     and return a value.
    /// </returns>
    ValueTask<IReadOnlyCollection<TResult>> PublishAsync<TResult>(object message, bool throwIfUnhandled = false);
}
