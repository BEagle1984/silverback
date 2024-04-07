// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Util;

namespace Silverback.Lock;

/// <summary>
///     This implementation of <see cref="DistributedLock" /> is not really distributed and is meant for testing purposes only.
/// </summary>
public sealed class InMemoryLock : DistributedLock, IDisposable
{
    private readonly DistributedLockSettings _settings;

    private readonly ISilverbackLogger<InMemoryLock> _silverbackLogger;

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    ///     Initializes a new instance of the <see cref="InMemoryLock" /> class.
    /// </summary>
    /// <param name="settings">
    ///     The lock settings.
    /// </param>
    /// <param name="logger">
    ///     The logger.
    /// </param>
    public InMemoryLock(DistributedLockSettings settings, ISilverbackLogger<InMemoryLock> logger)
    {
        _settings = Check.NotNull(settings, nameof(settings));
        _silverbackLogger = Check.NotNull(logger, nameof(logger));
    }

    /// <inheritdoc cref="IDisposable.Dispose" />
    public void Dispose() => _semaphore.Dispose();

    /// <inheritdoc cref="DistributedLock.AcquireCoreAsync" />
    protected override async ValueTask<DistributedLockHandle> AcquireCoreAsync(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new InMemoryLockHandle(_settings.LockName, _semaphore, _silverbackLogger);
    }

    private sealed class InMemoryLockHandle : DistributedLockHandle
    {
        private readonly string _lockName;

        private readonly SemaphoreSlim _semaphore;

        private readonly ISilverbackLogger<InMemoryLock> _logger;

        private bool _isDisposed;

        public InMemoryLockHandle(string lockName, SemaphoreSlim semaphore, ISilverbackLogger<InMemoryLock> logger)
        {
            _lockName = lockName;
            _semaphore = semaphore;
            _logger = logger;

            _logger.LogLockAcquired(_lockName);
        }

        public override CancellationToken LockLostToken => CancellationToken.None;

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _semaphore.Release();

            _logger.LogLockReleased(_lockName);
        }

        [SuppressMessage("", "VSTHRD103", Justification = "Intentional")]
        [SuppressMessage("Performance", "CA1849:Call async methods when in an async method", Justification = "Reviewed")]
        protected override ValueTask DisposeCoreAsync()
        {
            Dispose(true);
            return ValueTaskFactory.CompletedTask;
        }
    }
}
