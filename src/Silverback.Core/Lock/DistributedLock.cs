// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Lock;

/// <summary>
///     The base class for all distributed locks.
/// </summary>
public abstract class DistributedLock : IDistributedLock
{
    /// <inheritdoc cref="IDistributedLock.AcquireAsync" />
    public ValueTask<DistributedLockHandle> AcquireAsync(CancellationToken cancellationToken = default) =>
        AcquireCoreAsync(cancellationToken);

    /// <inheritdoc cref="IDistributedLock.AcquireAsync" />
    protected abstract ValueTask<DistributedLockHandle> AcquireCoreAsync(CancellationToken cancellationToken);
}
