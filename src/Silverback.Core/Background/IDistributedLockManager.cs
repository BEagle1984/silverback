// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Background
{
    /// <summary>
    ///     Implements a lock mechanism that relies on a shared persisted storage (such as a database) to
    ///     synchronize different processes.
    /// </summary>
    public interface IDistributedLockManager
    {
        /// <summary>
        ///     Acquires a new lock on the specified resource.
        /// </summary>
        /// <param name="settings">
        ///     Specifies all settings of the lock to be acquired.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
        ///     acquired <see cref="DistributedLock" /> (or <c>null</c> if no lock was actually acquired).
        /// </returns>
        Task<DistributedLock?> AcquireAsync(DistributedLockSettings settings, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Returns a boolean value indicating whether the specified lock is taken already.
        /// </summary>
        /// <param name="settings">
        ///     Specifies the lock to be checked.
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains a
        ///     boolean value indicating whether the lock is taken.
        /// </returns>
        Task<bool> CheckIsStillLockedAsync(DistributedLockSettings settings);

        /// <summary>
        ///     Called periodically after the lock has been acquired to send an heartbeat that keeps the lock.
        /// </summary>
        /// <param name="settings">
        ///     Specifies the lock to be refreshed.
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains a
        ///     boolean value indicating whether the lock could be refreshed.
        /// </returns>
        Task<bool> SendHeartbeatAsync(DistributedLockSettings settings);

        /// <summary>
        ///     Releases the lock.
        /// </summary>
        /// <param name="settings">
        ///     Specifies the lock to be released.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task ReleaseAsync(DistributedLockSettings settings);
    }
}
