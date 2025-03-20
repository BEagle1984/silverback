// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Publishing;

/// <summary>
///     Adds the <see cref="ExecuteQuery{TResult}" /> and <see cref="ExecuteQueryAsync{TResult}(IPublisher,IQuery{TResult},CancellationToken)" /> methods to the <see cref="IPublisher" /> interface.
/// </summary>
public static class QueryPublisherExtensions
{
    /// <summary>
    ///     Executes the specified query forwarding it to its subscribers via the message bus and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result that is expected to be returned by the subscribers.
    /// </typeparam>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="queryMessage">
    ///     The query to be executed.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    /// <returns>
    ///     The query result.
    /// </returns>
    public static TResult ExecuteQuery<TResult>(this IPublisher publisher, IQuery<TResult> queryMessage, bool throwIfUnhandled = true) =>
        Check.NotNull(publisher, nameof(publisher)).Publish<TResult>(queryMessage, throwIfUnhandled).Single();

    /// <summary>
    ///     Executes the specified query forwarding it to its subscribers via the message bus and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result that is expected to be returned by the subscribers.
    /// </typeparam>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="queryMessage">
    ///     The query to be executed.
    /// </param>
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The ValueTask result contains the
    ///     query result.
    /// </returns>
    public static async ValueTask<TResult> ExecuteQueryAsync<TResult>(
        this IPublisher publisher,
        IQuery<TResult> queryMessage,
        CancellationToken cancellationToken = default) =>
        (await Check.NotNull(publisher, nameof(publisher)).PublishAsync<TResult>(queryMessage, true, cancellationToken).ConfigureAwait(false))
        .Single();

    /// <summary>
    ///     Executes the specified query forwarding it to its subscribers via the message bus and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the result that is expected to be returned by the subscribers.
    /// </typeparam>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
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
    public static async ValueTask<TResult> ExecuteQueryAsync<TResult>(
        this IPublisher publisher,
        IQuery<TResult> queryMessage,
        bool throwIfUnhandled,
        CancellationToken cancellationToken = default) =>
        (await Check.NotNull(publisher, nameof(publisher)).PublishAsync<TResult>(queryMessage, throwIfUnhandled, cancellationToken).ConfigureAwait(false))
        .Single();
}
