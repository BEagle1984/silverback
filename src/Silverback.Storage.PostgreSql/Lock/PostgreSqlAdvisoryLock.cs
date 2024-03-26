// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Medallion.Threading.Postgres;
using Silverback.Diagnostics;
using Silverback.Util;

namespace Silverback.Lock;

/// <summary>
///     The distributed lock based on PostgreSql Advisory locks.
/// </summary>
public sealed class PostgreSqlAdvisoryLock : DistributedLock
{
    private readonly PostgreSqlAdvisoryLockSettings _settings;

    private readonly ISilverbackLogger<PostgreSqlAdvisoryLock> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PostgreSqlAdvisoryLock" /> class.
    /// </summary>
    /// <param name="settings">
    ///     The lock settings.
    /// </param>
    /// <param name="logger">
    ///    The logger.
    /// </param>
    public PostgreSqlAdvisoryLock(PostgreSqlAdvisoryLockSettings settings, ISilverbackLogger<PostgreSqlAdvisoryLock> logger)
    {
        _settings = Check.NotNull(settings, nameof(settings));
        _logger = Check.NotNull(logger, nameof(logger));
    }

    /// <inheritdoc cref="DistributedLock.AcquireCoreAsync" />
    protected override async ValueTask<DistributedLockHandle> AcquireCoreAsync(CancellationToken cancellationToken)
    {
        PostgresDistributedLock distributedLock = new(new PostgresAdvisoryLockKey(_settings.LockName, true), _settings.ConnectionString);

        return new PostgreSqlAdvisoryLockHandle(
            _settings.LockName,
            await distributedLock.AcquireAsync(cancellationToken: cancellationToken).ConfigureAwait(false),
            _logger);
    }

    private sealed class PostgreSqlAdvisoryLockHandle : DistributedLockHandle
    {
        private readonly string _lockName;

        private readonly PostgresDistributedLockHandle _innerHandle;

        private readonly ISilverbackLogger<PostgreSqlAdvisoryLock> _logger;

        private bool _isDisposed;

        public PostgreSqlAdvisoryLockHandle(string lockName, PostgresDistributedLockHandle innerHandle, ISilverbackLogger<PostgreSqlAdvisoryLock> logger)
        {
            _lockName = lockName;
            _innerHandle = innerHandle;
            _logger = logger;

            _logger.LogLockAcquired(_lockName);
            _innerHandle.HandleLostToken.Register(() => _logger.LogLockLost(_lockName));
        }

        public override CancellationToken LockLostToken => _innerHandle.HandleLostToken;

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed || !disposing)
                return;

            _isDisposed = true;
            _innerHandle.Dispose();

            _logger.LogLockReleased(_lockName);
        }

        protected override ValueTask DisposeCoreAsync() => _innerHandle.DisposeAsync();
    }
}
