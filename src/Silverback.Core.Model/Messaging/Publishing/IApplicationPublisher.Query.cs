// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing;

/// <content>
///     Defines the <c>ExecuteQuery</c>/<c>ExecuteQueryAsync</c> methods.
/// </content>
public partial interface IApplicationPublisher
{
    /// <summary>
    ///     Executes the specified query forwarding it to its subscribers via the message bus and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result that is expected to be returned by the subscribers.
    /// </typeparam>
    /// <param name="queryMessage">
    ///     The query to be executed.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    /// <returns>
    ///     The query result.
    /// </returns>
    TResult ExecuteQuery<TResult>(IQuery<TResult> queryMessage, bool throwIfUnhandled = true);

    /// <summary>
    ///     Executes the specified query forwarding it to its subscribers via the message bus and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result that is expected to be returned by the subscribers.
    /// </typeparam>
    /// <param name="queryMessage">
    ///     The query to be executed.
    /// </param>
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation. The ValueTask result contains the
    ///     query result.
    /// </returns>
    ValueTask<TResult> ExecuteQueryAsync<TResult>(IQuery<TResult> queryMessage, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Executes the specified query forwarding it to its subscribers via the message bus and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result that is expected to be returned by the subscribers.
    /// </typeparam>
    /// <param name="queryMessage">
    ///     The query to be executed.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The ValueTask result contains the
    ///     query result.
    /// </returns>
    ValueTask<TResult> ExecuteQueryAsync<TResult>(
        IQuery<TResult> queryMessage,
        bool throwIfUnhandled,
        CancellationToken cancellationToken = default);
}
