// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing;

/// <summary>
///     Publishes the messages implementing <see cref="ICommand" /> or <see cref="ICommand{TResult}" />.
/// </summary>
public interface ICommandPublisher
{
    /// <summary>
    ///     Executes the specified command publishing it to the internal bus. The message will be forwarded to its subscribers and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <param name="commandMessage">
    ///     The command to be executed.
    /// </param>
    void Execute(ICommand commandMessage);

    /// <summary>
    ///     Executes the specified command publishing it to the internal bus. The message will be forwarded to its subscribers and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result that is expected to be returned by the subscribers.
    /// </typeparam>
    /// <param name="commandMessage">
    ///     The command to be executed.
    /// </param>
    /// <returns>
    ///     The command result.
    /// </returns>
    TResult Execute<TResult>(ICommand<TResult> commandMessage);

    /// <summary>
    ///     Executes the specified command publishing it to the internal bus. The message will be forwarded to its subscribers and the
    ///     <see cref="Task" /> will not complete until all subscribers have processed it (unless using Silverback.Integration to produce
    ///     and consume the message through a message broker).
    /// </summary>
    /// <param name="commandMessage">
    ///     The command to be executed.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task ExecuteAsync(ICommand commandMessage);

    /// <summary>
    ///     Executes the specified command publishing it to the internal bus. The message will be forwarded to its subscribers and the
    ///     <see cref="Task" /> will not complete until all subscribers have processed it (unless using Silverback.Integration to produce
    ///     and consume the message through a message broker).
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result that is expected to be returned by the subscribers.
    /// </typeparam>
    /// <param name="commandMessage">
    ///     The command to be executed.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
    ///     command result.
    /// </returns>
    Task<TResult> ExecuteAsync<TResult>(ICommand<TResult> commandMessage);
}
