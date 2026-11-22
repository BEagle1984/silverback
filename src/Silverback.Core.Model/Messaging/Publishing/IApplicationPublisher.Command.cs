// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Publishing;

/// <content>
///     Defines the <c>ExecuteCommand</c>/<c>ExecuteCommandAsync</c> methods.
/// </content>
public partial interface IApplicationPublisher
{
    /// <summary>
    ///     Executes the specified command forwarding it to its subscribers via the message bus and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <param name="commandMessage">
    ///     The command to be executed.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    /// <exception cref="UnhandledMessageException">
    ///     <paramref name="throwIfUnhandled" /> is <c>true</c> and no subscriber handled the command.
    /// </exception>
    /// <exception cref="SubscribedMethodInvocationException">
    ///     A subscribed method has an invalid signature, for example no argument resolver can be found for one of its parameters.
    /// </exception>
    void ExecuteCommand(ICommand commandMessage, bool throwIfUnhandled = true);

    /// <summary>
    ///     Executes the specified command forwarding it to its subscribers via the message bus and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result that is expected to be returned by the subscribers.
    /// </typeparam>
    /// <param name="commandMessage">
    ///     The command to be executed.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    /// <returns>
    ///     The command result.
    /// </returns>
    /// <exception cref="UnhandledMessageException">
    ///     <paramref name="throwIfUnhandled" /> is <c>true</c> and no subscriber handled the command.
    /// </exception>
    /// <exception cref="SubscribedMethodInvocationException">
    ///     A subscribed method has an invalid signature, for example no argument resolver can be found for one of its parameters.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     The command wasn't handled by exactly one subscriber returning a result.
    /// </exception>
    TResult ExecuteCommand<TResult>(ICommand<TResult> commandMessage, bool throwIfUnhandled = true);

    /// <summary>
    ///     Executes the specified command forwarding it to its subscribers via the message bus and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <param name="commandMessage">
    ///     The command to be executed.
    /// </param>
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    /// <exception cref="UnhandledMessageException">
    ///     No subscriber handled the command.
    /// </exception>
    /// <exception cref="SubscribedMethodInvocationException">
    ///     A subscribed method has an invalid signature, for example no argument resolver can be found for one of its parameters.
    /// </exception>
    Task ExecuteCommandAsync(ICommand commandMessage, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Executes the specified command forwarding it to its subscribers via the message bus and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <param name="commandMessage">
    ///     The command to be executed.
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
    /// <exception cref="UnhandledMessageException">
    ///     <paramref name="throwIfUnhandled" /> is <c>true</c> and no subscriber handled the command.
    /// </exception>
    /// <exception cref="SubscribedMethodInvocationException">
    ///     A subscribed method has an invalid signature, for example no argument resolver can be found for one of its parameters.
    /// </exception>
    Task ExecuteCommandAsync(ICommand commandMessage, bool throwIfUnhandled, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Executes the specified command forwarding it to its subscribers via the message bus and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result that is expected to be returned by the subscribers.
    /// </typeparam>
    /// <param name="commandMessage">
    ///     The command to be executed.
    /// </param>
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The Task result contains the
    ///     command result.
    /// </returns>
    /// <exception cref="UnhandledMessageException">
    ///     No subscriber handled the command.
    /// </exception>
    /// <exception cref="SubscribedMethodInvocationException">
    ///     A subscribed method has an invalid signature, for example no argument resolver can be found for one of its parameters.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     The command wasn't handled by exactly one subscriber returning a result.
    /// </exception>
    Task<TResult> ExecuteCommandAsync<TResult>(ICommand<TResult> commandMessage, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Executes the specified command forwarding it to its subscribers via the message bus and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result that is expected to be returned by the subscribers.
    /// </typeparam>
    /// <param name="commandMessage">
    ///     The command to be executed.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The Task result contains the
    ///     command result.
    /// </returns>
    /// <exception cref="UnhandledMessageException">
    ///     <paramref name="throwIfUnhandled" /> is <c>true</c> and no subscriber handled the command.
    /// </exception>
    /// <exception cref="SubscribedMethodInvocationException">
    ///     A subscribed method has an invalid signature, for example no argument resolver can be found for one of its parameters.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     The command wasn't handled by exactly one subscriber returning a result.
    /// </exception>
    Task<TResult> ExecuteCommandAsync<TResult>(
        ICommand<TResult> commandMessage,
        bool throwIfUnhandled,
        CancellationToken cancellationToken = default);
}
