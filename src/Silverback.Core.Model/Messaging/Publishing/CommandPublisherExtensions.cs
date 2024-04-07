// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Publishing;

/// <summary>
///     Adds the <c>ExecuteCommand</c>, <c>ExecuteCommands</c>, <c>ExecuteCommandAsync</c> and <c>ExecuteCommandsAsync</c> methods to the
///     <see cref="IPublisher" /> interface.
/// </summary>
public static class CommandPublisherExtensions
{
    /// <summary>
    ///     Executes the specified command publishing it to the internal bus. The message will be forwarded to its subscribers and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="commandMessage">
    ///     The command to be executed.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    public static void ExecuteCommand(this IPublisher publisher, ICommand commandMessage, bool throwIfUnhandled = true) =>
        Check.NotNull(publisher, nameof(publisher)).Publish(commandMessage, throwIfUnhandled);

    /// <summary>
    ///     Executes the specified command publishing it to the internal bus. The message will be forwarded to its subscribers and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="commandMessage">
    ///     The command to be executed.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    public static void ExecuteCommand(this IPublisher publisher, IMessageWrapper<ICommand> commandMessage, bool throwIfUnhandled = true) =>
        Check.NotNull(publisher, nameof(publisher)).Publish(commandMessage, throwIfUnhandled);

    /// <summary>
    ///     Executes the specified command publishing it to the internal bus. The message will be forwarded to its subscribers and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result that is expected to be returned by the subscribers.
    /// </typeparam>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="commandMessage">
    ///     The command to be executed.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    /// <returns>
    ///     The command result.
    /// </returns>
    public static TResult ExecuteCommand<TResult>(this IPublisher publisher, ICommand<TResult> commandMessage, bool throwIfUnhandled = true) =>
        Check.NotNull(publisher, nameof(publisher)).Publish<TResult>(commandMessage, throwIfUnhandled).Single();

    /// <summary>
    ///     Executes the specified command publishing it to the internal bus. The message will be forwarded to its subscribers and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result that is expected to be returned by the subscribers.
    /// </typeparam>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="commandMessage">
    ///     The command to be executed.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    /// <returns>
    ///     The command result.
    /// </returns>
    public static TResult ExecuteCommand<TResult>(
        this IPublisher publisher,
        IMessageWrapper<ICommand<TResult>> commandMessage,
        bool throwIfUnhandled = true) =>
        Check.NotNull(publisher, nameof(publisher)).Publish<TResult>(commandMessage, throwIfUnhandled).Single();

    /// <summary>
    ///     Executes the specified commands publishing them to the internal bus. The messages will be forwarded to their subscribers and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="commandMessages">
    ///     The commands to be executed.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    public static void ExecuteCommands(this IPublisher publisher, IEnumerable<ICommand> commandMessages, bool throwIfUnhandled = true) =>
        Check.NotNull(publisher, nameof(publisher)).Publish(commandMessages, throwIfUnhandled);

    /// <summary>
    ///     Executes the specified commands publishing them to the internal bus. The messages will be forwarded to their subscribers and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="commandMessages">
    ///     The commands to be executed.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    public static void ExecuteCommands(
        this IPublisher publisher,
        IEnumerable<IMessageWrapper<ICommand>> commandMessages,
        bool throwIfUnhandled = true) =>
        Check.NotNull(publisher, nameof(publisher)).Publish(commandMessages, throwIfUnhandled);

    /// <summary>
    ///     Executes the specified commands publishing them to the internal bus. The messages will be forwarded to their subscribers and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="commandMessages">
    ///     The commands to be executed.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    public static void ExecuteCommands(this IPublisher publisher, IAsyncEnumerable<ICommand> commandMessages, bool throwIfUnhandled = true) =>
        Check.NotNull(publisher, nameof(publisher)).Publish(commandMessages, throwIfUnhandled);

    /// <summary>
    ///     Executes the specified commands publishing them to the internal bus. The messages will be forwarded to their subscribers and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="commandMessages">
    ///     The commands to be executed.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    public static void ExecuteCommands(
        this IPublisher publisher,
        IAsyncEnumerable<IMessageWrapper<ICommand>> commandMessages,
        bool throwIfUnhandled = true) =>
        Check.NotNull(publisher, nameof(publisher)).Publish(commandMessages, throwIfUnhandled);

    /// <summary>
    ///     Executes the specified commands publishing them to the internal bus. The messages will be forwarded to their subscribers and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result that is expected to be returned by the subscribers.
    /// </typeparam>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="commandMessages">
    ///     The commands to be executed.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    /// <returns>
    ///     The commands result.
    /// </returns>
    public static TResult ExecuteCommands<TResult>(
        this IPublisher publisher,
        IEnumerable<ICommand<TResult>> commandMessages,
        bool throwIfUnhandled = true) =>
        Check.NotNull(publisher, nameof(publisher)).Publish<TResult>(commandMessages, throwIfUnhandled).Single();

    /// <summary>
    ///     Executes the specified commands publishing them to the internal bus. The messages will be forwarded to their subscribers and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result that is expected to be returned by the subscribers.
    /// </typeparam>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="commandMessages">
    ///     The commands to be executed.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    /// <returns>
    ///     The commands result.
    /// </returns>
    public static TResult ExecuteCommands<TResult>(
        this IPublisher publisher,
        IEnumerable<IMessageWrapper<ICommand<TResult>>> commandMessages,
        bool throwIfUnhandled = true) =>
        Check.NotNull(publisher, nameof(publisher)).Publish<TResult>(commandMessages, throwIfUnhandled).Single();

    /// <summary>
    ///     Executes the specified commands publishing them to the internal bus. The messages will be forwarded to their subscribers and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result that is expected to be returned by the subscribers.
    /// </typeparam>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="commandMessages">
    ///     The commands to be executed.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    /// <returns>
    ///     The commands result.
    /// </returns>
    public static TResult ExecuteCommands<TResult>(
        this IPublisher publisher,
        IAsyncEnumerable<ICommand<TResult>> commandMessages,
        bool throwIfUnhandled = true) =>
        Check.NotNull(publisher, nameof(publisher)).Publish<TResult>(commandMessages, throwIfUnhandled).Single();

    /// <summary>
    ///     Executes the specified commands publishing them to the internal bus. The messages will be forwarded to their subscribers and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result that is expected to be returned by the subscribers.
    /// </typeparam>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="commandMessages">
    ///     The commands to be executed.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    /// <returns>
    ///     The commands result.
    /// </returns>
    public static TResult ExecuteCommands<TResult>(
        this IPublisher publisher,
        IAsyncEnumerable<IMessageWrapper<ICommand<TResult>>> commandMessages,
        bool throwIfUnhandled = true) =>
        Check.NotNull(publisher, nameof(publisher)).Publish<TResult>(commandMessages, throwIfUnhandled).Single();

    /// <summary>
    ///     Executes the specified command publishing it to the internal bus. The message will be forwarded to its subscribers and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="commandMessage">
    ///     The command to be executed.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    public static ValueTask ExecuteCommandAsync(this IPublisher publisher, ICommand commandMessage, bool throwIfUnhandled = true) =>
        Check.NotNull(publisher, nameof(publisher)).PublishAsync(commandMessage, throwIfUnhandled);

    /// <summary>
    ///     Executes the specified command publishing it to the internal bus. The message will be forwarded to its subscribers and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="commandMessage">
    ///     The command to be executed.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    public static ValueTask ExecuteCommandAsync(
        this IPublisher publisher,
        IMessageWrapper<ICommand> commandMessage,
        bool throwIfUnhandled = true) =>
        Check.NotNull(publisher, nameof(publisher)).PublishAsync(commandMessage, throwIfUnhandled);

    /// <summary>
    ///     Executes the specified command publishing it to the internal bus. The message will be forwarded to its subscribers and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result that is expected to be returned by the subscribers.
    /// </typeparam>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="commandMessage">
    ///     The command to be executed.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The ValueTask result contains the
    ///     command result.
    /// </returns>
    public static async ValueTask<TResult> ExecuteCommandAsync<TResult>(
        this IPublisher publisher,
        ICommand<TResult> commandMessage,
        bool throwIfUnhandled = true) =>
        (await Check.NotNull(publisher, nameof(publisher)).PublishAsync<TResult>(commandMessage, throwIfUnhandled).ConfigureAwait(false)).Single();

    /// <summary>
    ///     Executes the specified command publishing it to the internal bus. The message will be forwarded to its subscribers and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result that is expected to be returned by the subscribers.
    /// </typeparam>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="commandMessage">
    ///     The command to be executed.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The ValueTask result contains the
    ///     command result.
    /// </returns>
    public static async ValueTask<TResult> ExecuteCommandAsync<TResult>(
        this IPublisher publisher,
        IMessageWrapper<ICommand<TResult>> commandMessage,
        bool throwIfUnhandled = true) =>
        (await Check.NotNull(publisher, nameof(publisher)).PublishAsync<TResult>(commandMessage, throwIfUnhandled).ConfigureAwait(false)).Single();

    /// <summary>
    ///     Executes the specified commands publishing them to the internal bus. The messages will be forwarded to their subscribers and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="commandMessages">
    ///     The commands to be executed.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    public static ValueTask ExecuteCommandsAsync(
        this IPublisher publisher,
        IEnumerable<ICommand> commandMessages,
        bool throwIfUnhandled = true) =>
        Check.NotNull(publisher, nameof(publisher)).PublishAsync(commandMessages, throwIfUnhandled);

    /// <summary>
    ///     Executes the specified commands publishing them to the internal bus. The messages will be forwarded to their subscribers and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="commandMessages">
    ///     The commands to be executed.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    public static ValueTask ExecuteCommandsAsync(
        this IPublisher publisher,
        IEnumerable<IMessageWrapper<ICommand>> commandMessages,
        bool throwIfUnhandled = true) =>
        Check.NotNull(publisher, nameof(publisher)).PublishAsync(commandMessages, throwIfUnhandled);

    /// <summary>
    ///     Executes the specified commands publishing them to the internal bus. The messages will be forwarded to their subscribers and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="commandMessages">
    ///     The commands to be executed.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    public static ValueTask ExecuteCommandsAsync(
        this IPublisher publisher,
        IAsyncEnumerable<ICommand> commandMessages,
        bool throwIfUnhandled = true) =>
        Check.NotNull(publisher, nameof(publisher)).PublishAsync(commandMessages, throwIfUnhandled);

    /// <summary>
    ///     Executes the specified commands publishing them to the internal bus. The messages will be forwarded to their subscribers and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="commandMessages">
    ///     The commands to be executed.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    public static ValueTask ExecuteCommandsAsync(
        this IPublisher publisher,
        IAsyncEnumerable<IMessageWrapper<ICommand>> commandMessages,
        bool throwIfUnhandled = true) =>
        Check.NotNull(publisher, nameof(publisher)).PublishAsync(commandMessages, throwIfUnhandled);

    /// <summary>
    ///     Executes the specified commands publishing them to the internal bus. The messages will be forwarded to their subscribers and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result that is expected to be returned by the subscribers.
    /// </typeparam>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="commandMessages">
    ///     The commands to be executed.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The ValueTask result contains the
    ///     command result.
    /// </returns>
    public static async ValueTask<TResult> ExecuteCommandsAsync<TResult>(
        this IPublisher publisher,
        IEnumerable<ICommand<TResult>> commandMessages,
        bool throwIfUnhandled = true) =>
        (await Check.NotNull(publisher, nameof(publisher)).PublishAsync<TResult>(commandMessages, throwIfUnhandled).ConfigureAwait(false)).Single();

    /// <summary>
    ///     Executes the specified commands publishing them to the internal bus. The messages will be forwarded to their subscribers and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result that is expected to be returned by the subscribers.
    /// </typeparam>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="commandMessages">
    ///     The commands to be executed.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The ValueTask result contains the
    ///     command result.
    /// </returns>
    public static async ValueTask<TResult> ExecuteCommandsAsync<TResult>(
        this IPublisher publisher,
        IEnumerable<IMessageWrapper<ICommand<TResult>>> commandMessages,
        bool throwIfUnhandled = true) =>
        (await Check.NotNull(publisher, nameof(publisher)).PublishAsync<TResult>(commandMessages, throwIfUnhandled).ConfigureAwait(false)).Single();

    /// <summary>
    ///     Executes the specified commands publishing them to the internal bus. The messages will be forwarded to their subscribers and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result that is expected to be returned by the subscribers.
    /// </typeparam>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="commandMessages">
    ///     The commands to be executed.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The ValueTask result contains the
    ///     command result.
    /// </returns>
    public static async ValueTask<TResult> ExecuteCommandsAsync<TResult>(
        this IPublisher publisher,
        IAsyncEnumerable<ICommand<TResult>> commandMessages,
        bool throwIfUnhandled = true) =>
        (await Check.NotNull(publisher, nameof(publisher)).PublishAsync<TResult>(commandMessages, throwIfUnhandled).ConfigureAwait(false)).Single();

    /// <summary>
    ///     Executes the specified commands publishing them to the internal bus. The messages will be forwarded to their subscribers and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result that is expected to be returned by the subscribers.
    /// </typeparam>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="commandMessages">
    ///     The commands to be executed.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The ValueTask result contains the
    ///     command result.
    /// </returns>
    public static async ValueTask<TResult> ExecuteCommandsAsync<TResult>(
        this IPublisher publisher,
        IAsyncEnumerable<IMessageWrapper<ICommand<TResult>>> commandMessages,
        bool throwIfUnhandled = true) =>
        (await Check.NotNull(publisher, nameof(publisher)).PublishAsync<TResult>(commandMessages, throwIfUnhandled).ConfigureAwait(false)).Single();
}
