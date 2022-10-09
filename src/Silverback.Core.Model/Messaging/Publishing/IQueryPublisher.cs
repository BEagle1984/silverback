// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing;

/// <summary>
///     Publishes the messages implementing <see cref="IQuery{TResult}" />.
/// </summary>
public interface IQueryPublisher : IPublisherBase
{
    /// <summary>
    ///     Executes the specified query publishing it to the internal bus. The message will be forwarded to its subscribers and the
    ///     method will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume
    ///     the message through a message broker).
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the expected query result.
    /// </typeparam>
    /// <param name="queryMessage">
    ///     The query to be executed.
    /// </param>
    /// <returns>
    ///     The query result.
    /// </returns>
    TResult Execute<TResult>(IQuery<TResult> queryMessage);

    /// <summary>
    ///     Executes the specified query publishing it to the internal bus. The message will be forwarded to its subscribers and the
    ///     <see cref="ValueTask" /> will not complete until all subscribers have processed it (unless using Silverback.Integration to produce
    ///     and consume the message through a message broker).
    /// </summary>
    /// <typeparam name="TResult">
    ///     The type of the expected query result.
    /// </typeparam>
    /// <param name="queryMessage">
    ///     The query to be executed.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The ValueValueTask result contains the
    ///     query result.
    /// </returns>
    ValueTask<TResult> ExecuteAsync<TResult>(IQuery<TResult> queryMessage);
}
