// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Lock;

/// <summary>
///     Exposes the methods to acquire and release a lock.
/// </summary>
public interface IDistributedLock
{
    /// <summary>
    ///     Gets the lock settings.
    /// </summary>
    DistributedLockSettings Settings { get; }

    /// <summary>
    ///     Acquires the lock.
    /// </summary>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    ValueTask<DistributedLockHandle> AcquireAsync(CancellationToken cancellationToken = default);
}
