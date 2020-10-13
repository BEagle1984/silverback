// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;

namespace Silverback.Messaging.Inbound.Transaction
{
    /// <summary>
    ///     Handles the consumer transaction. It commits or rolls back both the offsets and the enlisted
    ///     transactional services (see <see cref="ITransactional" />).
    /// </summary>
    public interface IConsumerTransactionManager
    {
        /// <summary>
        ///     Adds the specified service to the transaction participants to be called upon commit or rollback.
        /// </summary>
        /// <param name="transactionalService">
        ///     The service to be enlisted.
        /// </param>
        void Enlist(ITransactional transactionalService);

        /// <summary>
        ///     Commits the transaction.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task CommitAsync();

        /// <summary>
        ///     Aborts the transaction and causes the uncommitted changes to be rolled back.
        /// </summary>
        /// <param name="exception">
        ///     The exception that caused the rollback.
        /// </param>
        /// <param name="commitOffsets">
        ///     A value indicating whether the offsets have to be committed anyway. This depends on the error policy
        ///     being applied.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task RollbackAsync(Exception? exception, bool commitOffsets = false);
    }
}
