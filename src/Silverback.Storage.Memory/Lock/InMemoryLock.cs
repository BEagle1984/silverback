// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Silverback.Util;

namespace Silverback.Lock;

/// <summary>
///     This implementation of <see cref="DistributedLock" /> is not really distributed and is meant for testing purposes only.
/// </summary>
public sealed class InMemoryLock : DistributedLock
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    ///     Initializes a new instance of the <see cref="InMemoryLock" /> class.
    /// </summary>
    /// <param name="settings">
    ///     The lock settings of any type.
    /// </param>
    public InMemoryLock(DistributedLockSettings settings)
        : base(settings)
    {
    }

    /// <inheritdoc cref="DistributedLock.AcquireCoreAsync" />
    protected override async ValueTask<DistributedLockHandle> AcquireCoreAsync(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new InMemoryLockHandle(_semaphore);
    }

    private sealed class InMemoryLockHandle : DistributedLockHandle
    {
        private readonly SemaphoreSlim _semaphore;

        private bool _isDisposed;

        public InMemoryLockHandle(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public override bool IsLost => false;

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _semaphore.Release();
        }

        protected override ValueTask DisposeCoreAsync()
        {
            Dispose(false);
            return ValueTaskFactory.CompletedTask;
        }
    }
}
