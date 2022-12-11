// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;

namespace Silverback.Messaging.Consuming.Transaction;

/// <summary>
///     Declares the <c>CommitAsync</c> and <c>RollbackAsync</c> methods, allowing the service to be enlisted
///     into the consumer transaction (see <see cref="ConsumerTransactionManager" />).
/// </summary>
// TODO: USED?
public interface ITransactional
{
    /// <summary>
    ///     Called when the message has been successfully processed to commit the transaction.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task CommitAsync();

    /// <summary>
    ///     Called when an exception occurs during the message processing to rollback the transaction.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task RollbackAsync();
}
