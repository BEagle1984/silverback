// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Util;

namespace Silverback.Lock;

/// <summary>
///     The base class for the table-based locks.
/// </summary>
public abstract class TableBasedDistributedLock : DistributedLock
{
    private readonly TableBasedDistributedLockSettings _settings;

    private readonly ISilverbackLogger<TableBasedDistributedLock> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TableBasedDistributedLock" /> class.
    /// </summary>
    /// <param name="settings">
    ///     The lock settings.
    /// </param>
    /// <param name="logger">
    ///     The logger.
    /// </param>
    protected TableBasedDistributedLock(TableBasedDistributedLockSettings settings, ISilverbackLogger<TableBasedDistributedLock> logger)
    {
        _settings = Check.NotNull(settings, nameof(settings));
        _logger = Check.NotNull(logger, nameof(logger));
    }

    /// <inheritdoc cref="DistributedLock.AcquireCoreAsync" />
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception is logged")]
    protected override async ValueTask<DistributedLockHandle> AcquireCoreAsync(CancellationToken cancellationToken)
    {
        string handlerName = Guid.NewGuid().ToString("N");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (await TryAcquireLockAsync(handlerName).ConfigureAwait(true))
                {
                    return new TableLockHandle(
                        _settings,
                        handlerName,
                        UpdateHeartbeatAsync,
                        ReleaseLockAsync,
                        _logger);
                }
            }
            catch (Exception ex)
            {
                _logger.LogAcquireLockFailed(_settings.LockName, ex);
            }

            await Task.Delay(_settings.AcquireInterval, cancellationToken).ConfigureAwait(false);
        }

        throw new OperationCanceledException(cancellationToken);
    }

    /// <summary>
    ///     Tries to acquire the lock.
    /// </summary>
    /// <param name="handlerName">
    ///     The name of the current handler.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation. The result is <c>true</c> if the lock was acquired, otherwise <c>false</c>.
    /// </returns>
    protected abstract Task<bool> TryAcquireLockAsync(string handlerName);

    /// <summary>
    ///     Updates the last heartbeat of the lock.
    /// </summary>
    /// <param name="handlerName">
    ///     The name of the current handler.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    protected abstract Task UpdateHeartbeatAsync(string handlerName);

    /// <summary>
    ///     Releases the lock.
    /// </summary>
    /// <param name="handlerName">
    ///     The name of the current handler.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    protected abstract Task ReleaseLockAsync(string handlerName);

    private sealed class TableLockHandle : DistributedLockHandle
    {
        private readonly TableBasedDistributedLockSettings _settings;

        private readonly string _handlerName;

        private readonly Func<string, Task> _updateHeartbeatAsync;

        private readonly Func<string, Task> _releaseLockAsync;

        private readonly ISilverbackLogger<TableBasedDistributedLock> _logger;

        [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "False positive")]
        private readonly CancellationTokenSource _lockLostTokenSource = new();

        [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "False positive")]
        private readonly CancellationTokenSource _disposeTokenSource = new();

        private bool _isDisposed;

        public TableLockHandle(
            TableBasedDistributedLockSettings settings,
            string handlerName,
            Func<string, Task> updateHeartbeatAsync,
            Func<string, Task> releaseLockAsync,
            ISilverbackLogger<TableBasedDistributedLock> logger)
        {
            _settings = settings;
            _handlerName = handlerName;
            _updateHeartbeatAsync = updateHeartbeatAsync;
            _releaseLockAsync = releaseLockAsync;
            _logger = logger;

            Task.Run(HeartbeatAsync).FireAndForget();
        }

        public override CancellationToken LockLostToken => _lockLostTokenSource.Token;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                DisposeCoreAsync().SafeWait();
        }

        protected override async ValueTask DisposeCoreAsync()
        {
            if (_isDisposed)
                return;

#if NETSTANDARD
            _disposeTokenSource.Cancel();
#else
            await _disposeTokenSource.CancelAsync().ConfigureAwait(false);
#endif

            await ReleaseLockAsync().ConfigureAwait(false);

            _isDisposed = true;

            _lockLostTokenSource.Dispose();
            _disposeTokenSource.Dispose();
        }

        private async Task HeartbeatAsync()
        {
            while (!_disposeTokenSource.Token.IsCancellationRequested)
            {
                if (!await UpdateHeartbeatAsync().ConfigureAwait(false))
                    return;

                await Task.Delay(_settings.HeartbeatInterval, _disposeTokenSource.Token).ConfigureAwait(false);
            }
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception is logged")]
        private async Task<bool> UpdateHeartbeatAsync()
        {
            try
            {
                await _updateHeartbeatAsync(_handlerName).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (!_disposeTokenSource.Token.IsCancellationRequested)
                {
                    _logger.LogLockLost(_settings.LockName, ex);

#if NETSTANDARD
                    _lockLostTokenSource.Cancel();
#else
                    await _lockLostTokenSource.CancelAsync().ConfigureAwait(false);
#endif
                }

                return false;
            }

            return true;
        }

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception is logged")]
        private async Task ReleaseLockAsync()
        {
            try
            {
                await _releaseLockAsync(_handlerName).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogReleaseLockFailed(_settings.LockName, ex);
            }
        }
    }
}
