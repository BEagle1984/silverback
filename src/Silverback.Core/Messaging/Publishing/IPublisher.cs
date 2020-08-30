// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Messaging.Publishing
{
    /// <summary>
    ///     <para>
    ///         Publishes the messages to the internal bus.
    ///     </para>
    ///     <para>
    ///         This is the actual mediator that forwards the messages being published to their subscribers.
    ///     </para>
    /// </summary>
    public interface IPublisher
    {
        /// <summary>
        ///     Publishes the specified message to the internal bus. The message will be forwarded to its
        ///     subscribers and the method will not complete until all subscribers have processed it (unless using
        ///     Silverback.Integration to produce and consume the message through a message broker).
        /// </summary>
        /// <param name="message">
        ///     The message to be published.
        /// </param>
        void Publish(object message);

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
        void Publish(object message, bool throwIfUnhandled);

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
        /// <returns>
        ///     A collection of <typeparamref name="TResult" />, since multiple subscribers could handle the message
        ///     and return a value.
        /// </returns>
        IReadOnlyCollection<TResult> Publish<TResult>(object message);

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
        IReadOnlyCollection<TResult> Publish<TResult>(object message, bool throwIfUnhandled);

        /// <summary>
        ///     Publishes the specified messages to the internal bus. The messages will be forwarded to their
        ///     subscribers and the method will not complete until all subscribers have processed all messages
        ///     (unless using Silverback.Integration to produce and consume the messages through a message broker).
        /// </summary>
        /// <param name="messages">
        ///     The messages to be published.
        /// </param>
        void Publish(IEnumerable<object> messages);

        /// <summary>
        ///     Publishes the specified messages to the internal bus. The messages will be forwarded to their
        ///     subscribers and the method will not complete until all subscribers have processed all messages
        ///     (unless using Silverback.Integration to produce and consume the messages through a message broker).
        /// </summary>
        /// <param name="messages">
        ///     The messages to be published.
        /// </param>
        /// <param name="throwIfUnhandled">
        ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the
        ///     message.
        /// </param>
        void Publish(IEnumerable<object> messages, bool throwIfUnhandled);

        /// <summary>
        ///     Publishes the specified messages to the internal bus. The messages will be forwarded to their
        ///     subscribers and the method will not complete until all subscribers have processed all messages
        ///     (unless using Silverback.Integration to produce and consume the messages through a message broker).
        /// </summary>
        /// <typeparam name="TResult">
        ///     The type of the result that is expected to be returned by the subscribers.
        /// </typeparam>
        /// <param name="messages">
        ///     The messages to be published.
        /// </param>
        /// <returns>
        ///     A collection of <typeparamref name="TResult" />.
        /// </returns>
        IReadOnlyCollection<TResult> Publish<TResult>(IEnumerable<object> messages);

        /// <summary>
        ///     Publishes the specified messages to the internal bus. The messages will be forwarded to their
        ///     subscribers and the method will not complete until all subscribers have processed all messages
        ///     (unless using Silverback.Integration to produce and consume the messages through a message broker).
        /// </summary>
        /// <typeparam name="TResult">
        ///     The type of the result that is expected to be returned by the subscribers.
        /// </typeparam>
        /// <param name="messages">
        ///     The messages to be published.
        /// </param>
        /// <param name="throwIfUnhandled">
        ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the
        ///     message.
        /// </param>
        /// <returns>
        ///     A collection of <typeparamref name="TResult" />.
        /// </returns>
        IReadOnlyCollection<TResult> Publish<TResult>(IEnumerable<object> messages, bool throwIfUnhandled);

        /// <summary>
        ///     Publishes the specified message to the internal bus. The message will be forwarded to its
        ///     subscribers and the <see cref="Task" /> will not complete until all subscribers have processed it
        ///     (unless using Silverback.Integration to produce and consume the message through a message broker).
        /// </summary>
        /// <param name="message">
        ///     The message to be published.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task PublishAsync(object message);

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
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task PublishAsync(object message, bool throwIfUnhandled);

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
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains a
        ///     collection of <typeparamref name="TResult" />, since multiple subscribers could handle the message and
        ///     return a
        ///     value.
        /// </returns>
        Task<IReadOnlyCollection<TResult>> PublishAsync<TResult>(object message);

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
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains a
        ///     collection  of <typeparamref name="TResult" />, since multiple subscribers could handle the message
        ///     and return a
        ///     value.
        /// </returns>
        Task<IReadOnlyCollection<TResult>> PublishAsync<TResult>(object message, bool throwIfUnhandled);

        /// <summary>
        ///     Publishes the specified messages to the internal bus. The messages will be forwarded to their
        ///     subscribers and the <see cref="Task" /> will not complete until all subscribers have processed all
        ///     messages (unless using Silverback.Integration to produce and consume the messages through a message
        ///     broker).
        /// </summary>
        /// <param name="messages">
        ///     The messages to be published.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task PublishAsync(IEnumerable<object> messages);

        /// <summary>
        ///     Publishes the specified messages to the internal bus. The messages will be forwarded to their
        ///     subscribers and the <see cref="Task" /> will not complete until all subscribers have processed all
        ///     messages (unless using Silverback.Integration to produce and consume the messages through a message
        ///     broker).
        /// </summary>
        /// <param name="messages">
        ///     The messages to be published.
        /// </param>
        /// <param name="throwIfUnhandled">
        ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the
        ///     message.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task PublishAsync(IEnumerable<object> messages, bool throwIfUnhandled);

        /// <summary>
        ///     Publishes the specified messages to the internal bus. The messages will be forwarded to their
        ///     subscribers and the <see cref="Task" /> will not complete until all subscribers have processed all
        ///     messages (unless using Silverback.Integration to produce and consume the messages through a message
        ///     broker).
        /// </summary>
        /// <typeparam name="TResult">
        ///     The type of the result that is expected to be returned by the subscribers.
        /// </typeparam>
        /// <param name="messages">
        ///     The messages to be published.
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains a
        ///     collection of <typeparamref name="TResult" />.
        /// </returns>
        Task<IReadOnlyCollection<TResult>> PublishAsync<TResult>(IEnumerable<object> messages);

        /// <summary>
        ///     Publishes the specified messages to the internal bus. The messages will be forwarded to their
        ///     subscribers and the <see cref="Task" /> will not complete until all subscribers have processed all
        ///     messages (unless using Silverback.Integration to produce and consume the messages through a message
        ///     broker).
        /// </summary>
        /// <typeparam name="TResult">
        ///     The type of the result that is expected to be returned by the subscribers.
        /// </typeparam>
        /// <param name="messages">
        ///     The messages to be published.
        /// </param>
        /// <param name="throwIfUnhandled">
        ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the
        ///     message.
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains a
        ///     collection of <typeparamref name="TResult" />.
        /// </returns>
        Task<IReadOnlyCollection<TResult>> PublishAsync<TResult>(IEnumerable<object> messages, bool throwIfUnhandled);
    }
}
