// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Silverback.Util;

namespace Silverback.Lock;

/// <summary>
///     The base class for all distributed locks.
/// </summary>
public abstract class DistributedLock : IDistributedLock
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DistributedLock" /> class.
    /// </summary>
    /// <param name="settings">
    ///     The lock settings.
    /// </param>
    protected DistributedLock(DistributedLockSettings settings)
    {
        Settings = Check.NotNull(settings, nameof(settings));
    }

    /// <inheritdoc cref="IDistributedLock.Settings" />
    public DistributedLockSettings Settings { get; }

    /// <inheritdoc cref="IDistributedLock.AcquireAsync" />
    public ValueTask<DistributedLockHandle> AcquireAsync(CancellationToken cancellationToken = default) =>
        AcquireCoreAsync(cancellationToken);

    /// <inheritdoc cref="IDistributedLock.AcquireAsync" />
    protected abstract ValueTask<DistributedLockHandle> AcquireCoreAsync(CancellationToken cancellationToken);
}
