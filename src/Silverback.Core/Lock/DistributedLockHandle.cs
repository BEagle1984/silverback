// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;

namespace Silverback.Lock;

/// <summary>
///     A handle to the distributed lock. Dispose this to unlock.
/// </summary>
public abstract class DistributedLockHandle : IDisposable, IAsyncDisposable
{
    /// <summary>
    ///     Finalizes an instance of the <see cref="DistributedLockHandle" /> class.
    /// </summary>
    ~DistributedLockHandle()
    {
        Dispose(false);
    }

    /// <summary>
    ///     Gets a value indicating whether the lock was lost.
    /// </summary>
    public abstract bool IsLost { get; }

    /// <inheritdoc cref="IDisposable.Dispose" />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc cref="IAsyncDisposable.DisposeAsync" />
    public async ValueTask DisposeAsync()
    {
        await DisposeCoreAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <param name="disposing">
    ///     A value indicating whether the method has been called by the <c>Dispose</c> method and not from the finalizer.
    /// </param>
    protected abstract void Dispose(bool disposing);

    /// <inheritdoc cref="IAsyncDisposable.DisposeAsync" />
    protected abstract ValueTask DisposeCoreAsync();
}
