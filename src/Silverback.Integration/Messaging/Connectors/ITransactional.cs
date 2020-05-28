// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    ///     Declares the <c>
    ///         Commit
    ///     </c> and <c>
    ///         Rollback
    ///     </c> methods, allowing the service to be enlisted into the consumer transaction (see
    ///     <see cref="ConsumerTransactionManager" />).
    /// </summary>
    public interface ITransactional
    {
        /// <summary>
        ///     Called when the message has been successfully processed to commit the transaction.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the result of the asynchronous operation.
        /// </returns>
        Task Commit();

        /// <summary>
        ///     Called when an exception occurs during the message processing to rollback the transaction.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the result of the asynchronous operation.
        /// </returns>
        Task Rollback();
    }
}
